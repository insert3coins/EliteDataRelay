using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Lightweight HTTP/WebSocket server to expose browser-friendly overlays for OBS Browser Source.
    /// Endpoints:
    ///  - GET /               index page
    ///  - GET /info           info overlay page
    ///  - GET /cargo          cargo overlay page
    ///  - GET /api/state      current state JSON
    ///  - WS  /ws             pushes state JSON diffs
    /// </summary>
    public class WebOverlayServerService : IDisposable
    {
        private readonly HttpListener _listener = new();
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<WebSocket, bool> _sockets = new();

        // Simple shared state object
        private class OverlayState
        {
            public string Commander { get; set; } = string.Empty;
            public string Ship { get; set; } = string.Empty;
            public long Balance { get; set; }
            public int CargoCount { get; set; }
            public int? CargoCapacity { get; set; }
            public long SessionCargo { get; set; }
            public long SessionCredits { get; set; }
            public string ShipIconUrl { get; set; } = string.Empty;
            public SystemExplorationData? Exploration { get; set; }
            public ExplorationSessionData? ExplorationSession { get; set; }
            public System.Collections.Generic.List<WebCargoItem> Items { get; set; } = new();
            public string CargoBarText { get; set; } = string.Empty;
        }

        public class WebCargoItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private readonly OverlayState _state = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public bool IsRunning => _cts != null;

        public void Start()
        {
            if (IsRunning || !AppConfiguration.EnableWebOverlayServer) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                _listener.Prefixes.Clear();
                var port = AppConfiguration.WebOverlayPort;
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Logger.Info($"[WebOverlay] Failed to start HttpListener: {ex.Message}");
                Stop();
                return;
            }

            _ = Task.Run(() => AcceptLoopAsync(token), token);
            Logger.Verbose($"[WebOverlay] Listening on http://localhost:{AppConfiguration.WebOverlayPort}/");
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            _cts = null;
            try { _listener.Stop(); } catch { }
            foreach (var s in _sockets.Keys)
            {
                try { s.Abort(); } catch { }
            }
            _sockets.Clear();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Info($"[WebOverlay] Accept failed: {ex.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleContextAsync(ctx, token), token);
            }
        }

        private async Task HandleContextAsync(HttpListenerContext ctx, CancellationToken token)
        {
            try
            {
                if (ctx.Request.IsWebSocketRequest && ctx.Request.Url?.AbsolutePath == "/ws")
                {
                    var wsContext = await ctx.AcceptWebSocketAsync(null);
                    var socket = wsContext.WebSocket;
                    _sockets[socket] = true;
                    await SendFullStateAsync(socket, token);
                    await ReceiveLoopAsync(socket, token); // keep-alive until close
                    return;
                }

                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                if (path == "/" || path == "/index.html")
                {
                    await WriteHtmlAsync(ctx, BuildIndexHtml());
                }
                else if (path == "/info")
                {
                    await WriteHtmlAsync(ctx, BuildInfoHtml());
                }
                else if (path == "/cargo")
                {
                    await WriteHtmlAsync(ctx, BuildCargoHtml());
                }
                else if (path == "/ship-icon")
                {
                    await WriteHtmlAsync(ctx, BuildShipIconHtml());
                }
                else if (path == "/exploration")
                {
                    await WriteHtmlAsync(ctx, BuildExplorationHtml());
                }
                else if (path == "/api/state")
                {
                    var json = JsonSerializer.Serialize(_state, _jsonOptions);
                    await WriteJsonAsync(ctx, json);
                }
                else if (path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
                {
                    await TryServeStaticAsync(ctx, path);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    await WriteStringAsync(ctx, "Not Found", "text/plain");
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"[WebOverlay] HandleContext error: {ex.Message}");
            }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }

        private async Task ReceiveLoopAsync(WebSocket socket, CancellationToken token)
        {
            var buffer = new byte[1];
            try
            {
                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                }
            }
            catch { }
            finally
            {
                _sockets.TryRemove(socket, out _);
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { }
            }
        }

        private Task WriteHtmlAsync(HttpListenerContext ctx, string html) => WriteStringAsync(ctx, html, "text/html; charset=utf-8");
        private Task WriteJsonAsync(HttpListenerContext ctx, string json) => WriteStringAsync(ctx, json, "application/json");
        private async Task WriteStringAsync(HttpListenerContext ctx, string text, string contentType)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task TryServeStaticAsync(HttpListenerContext ctx, string path)
        {
            try
            {
                // basic static file serving limited to /images/*
                var root = AppDomain.CurrentDomain.BaseDirectory;
                var imagesDirLower = System.IO.Path.Combine(root, "images");
                var imagesDirUpper = System.IO.Path.Combine(root, "Images");
                var rel = path.Replace('/', System.IO.Path.DirectorySeparatorChar).TrimStart(System.IO.Path.DirectorySeparatorChar);
                var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, rel));
                if (!full.StartsWith(imagesDirLower, StringComparison.OrdinalIgnoreCase) && !full.StartsWith(imagesDirUpper, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 404;
                    await WriteStringAsync(ctx, "Not Found", "text/plain");
                    return;
                }
                if (!System.IO.File.Exists(full))
                {
                    ctx.Response.StatusCode = 404;
                    await WriteStringAsync(ctx, "Not Found", "text/plain");
                    return;
                }
                string contentType = full.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "application/octet-stream";
                byte[] data = await System.IO.File.ReadAllBytesAsync(full);
                ctx.Response.ContentType = contentType;
                ctx.Response.ContentLength64 = data.LongLength;
                await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                ctx.Response.StatusCode = 500;
                await WriteStringAsync(ctx, "Server Error", "text/plain");
            }
        }

        private string BuildIndexHtml()
        {
            var head = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Elite Data Relay Overlays</title>" +
                       "<style>body{font-family:" + CssFontFamily() + ";background:#000;color:#ddd;padding:20px}a{color:#0ff}</style></head>";
            var body = "<body><h2>Overlays</h2><ul>" +
                       "<li><a href=\"/info\">Info Overlay</a></li>" +
                       "<li><a href=\"/cargo\">Cargo Overlay</a></li>" +
                       "<li><a href=\"/ship-icon\">Ship Icon Overlay</a></li>" +
                       "<li><a href=\"/exploration\">Exploration Overlay</a></li>" +
                       "</ul></body></html>";
            return head + body;
        }

        private string BuildInfoHtml()
        {
            var css = string.Join("\n", new[]
            {
                "body{margin:0;background:rgba(0,0,0,0)}",
                ".card{width:320px;height:85px;font-family:" + CssFontFamily() + ";color:" + CssWhite() + ";background:" + CssBackground() + ";border:" + BorderWidth() + " solid " + CssBorderColor() + ";padding:10px 14px;box-sizing:border-box}",
                ".label{color:" + CssGrayText() + ";font-size:" + FontSmall() + "px}",
                ".value{color:" + CssCyan() + ";font-size:" + FontNormal() + "px}",
                ".orange{color:" + CssOrange() + "}"
            });
            var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Info Overlay</title>" +
                       "<style>" + css + "</style></head><body>" +
                       "<div class='card'>"+
                       "<div><span class='label'>CMDR:</span> <span id='cmdr' class='value'>...</span></div>"+
                       "<div><span class='label'>Ship:</span> <span id='ship' class='value'>...</span></div>"+
                       "<div><span class='label'>Balance:</span> <span id='bal' class='orange'>0 CR</span></div>"+
                       "</div>"+
                       "<script>const el=(id)=>document.getElementById(id);function apply(s){ if(!s) return; el('cmdr').textContent=s.commander||''; el('ship').textContent=s.ship||''; el('bal').textContent=(s.balance||0).toLocaleString()+' CR'; }"+
                       "fetch('/api/state').then(r=>r.json()).then(apply);let ws; function connect(){ ws=new WebSocket((location.protocol=='https:'?'wss://':'ws://')+location.host+'/ws'); ws.onmessage=(e)=>{ try{apply(JSON.parse(e.data));}catch{} }; ws.onclose=()=>setTimeout(connect,1000);} connect();"+
                       "</script></body></html>";
            return html;
        }

        private string BuildCargoHtml()
        {
            var css = string.Join("\n", new[]
            {
                "body{margin:0;background:rgba(0,0,0,0)}",
                ".card{width:280px;height:600px;position:relative;font-family:" + CssFontFamily() + ";color:" + CssWhite() + ";background:" + CssBackground() + ";border:" + BorderWidth() + " solid " + CssBorderColor() + ";padding:10px 12px;box-sizing:border-box;overflow:hidden}",
                ".row{display:flex;justify-content:space-between;align-items:center}",
                ".label{color:" + CssGrayText() + ";font-size:" + FontSmall() + "px}",
                ".value{color:" + CssCyan() + ";font-size:" + FontNormal() + "px}",
                ".orange{color:" + CssOrange() + "}",
                ".list{position:absolute;top:38px;bottom:68px;left:12px;right:12px;overflow:hidden}",
                ".item{display:flex;justify-content:space-between;color:" + CssWhite() + ";font-size:" + FontSmall() + "px;line-height:1.4}",
                ".footer{position:absolute;left:12px;right:12px;bottom:8px;color:" + CssGrayText() + ";font-size:" + FontSmall() + "px}",
                ".sep{height:1px;background:" + CssGrayText() + ";opacity:.5;margin:6px 0}"
            });
            var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Cargo Overlay</title>"+
                       "<style>" + css + "</style></head><body>"+
                       "<div class='card'>"+
                       "<div class='row'><div class='label'>Cargo:</div><div class='value' id='count'>0</div><div class='label' id='bar'>??????????</div></div>"+
                       "<div class='list' id='list'></div>"+
                       "<div class='footer'><div class='sep'></div><div class='row'><div class='label'>Session CR:</div><div class='orange' id='sesscr'>0</div></div><div class='row'><div class='label'>Session Cargo:</div><div class='value' id='sessc'>0</div></div></div>"+
                       "</div>"+
                       "<script>const q=(id)=>document.getElementById(id);function apply(s){ if(!s) return; const cap = (s.cargoCapacity==null)?'?':s.cargoCapacity; q('count').textContent=(s.cargoCount||0)+'/'+cap; q('bar').textContent=s.cargoBarText||''; q('sesscr').textContent=(s.sessionCredits||0).toLocaleString(); q('sessc').textContent=(s.sessionCargo||0); const list=q('list'); list.innerHTML=''; const items=s.items||[]; if(items.length===0){ const p=document.createElement('div'); p.className='label'; p.textContent='Cargo hold is empty.'; list.appendChild(p);} else { items.forEach(it=>{ const r=document.createElement('div'); r.className='item'; const name=document.createElement('div'); name.textContent=it.displayName||''; const c=document.createElement('div'); c.textContent=it.count; r.appendChild(name); r.appendChild(c); list.appendChild(r); }); } }"+
                       "fetch('/api/state').then(r=>r.json()).then(apply);let ws; function connect(){ ws=new WebSocket((location.protocol=='https:'?'wss://':'ws://')+location.host+'/ws'); ws.onmessage=(e)=>{ try{apply(JSON.parse(e.data));}catch{} }; ws.onclose=()=>setTimeout(connect,1000);} connect();"+
                       "</script></body></html>";
            return html;
        }

        private string BuildShipIconHtml()
        {
            var css = string.Join("\n", new[]
            {
                "body{margin:0;background:rgba(0,0,0,0)}",
                ".wrap{display:inline-block;width:320px;height:320px;background:" + CssBackground() + ";border:" + BorderWidth() + " solid " + CssBorderColor() + ";padding:6px;box-sizing:border-box}",
                "img{display:block;max-width:100%;max-height:100%}",
                ".name{font-family:" + CssFontFamily() + ";color:" + CssCyan() + ";text-align:center;margin-top:6px}"
            });
            var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Ship Icon Overlay</title>"+
                       "<style>" + css + "</style></head><body>"+
                       "<div class='wrap'><img id='icon' src='' alt='Ship Icon' /><div class='name' id='name'></div></div>"+
                       "<script>const q=(id)=>document.getElementById(id);function apply(s){ if(!s) return; if(s.shipIconUrl) q('icon').src=s.shipIconUrl; q('name').textContent=s.ship||''; }"+
                       "fetch('/api/state').then(r=>r.json()).then(apply);let ws; function connect(){ ws=new WebSocket((location.protocol=='https:'?'wss://':'ws://')+location.host+'/ws'); ws.onmessage=(e)=>{ try{apply(JSON.parse(e.data));}catch{} }; ws.onclose=()=>setTimeout(connect,1000);} connect();"+
                       "</script></body></html>";
            return html;
        }

        private string BuildExplorationHtml()
        {
            var css = string.Join("\n", new[]
            {
                "body{margin:0;background:rgba(0,0,0,0)}",
                ".card{width:340px;height:195px;font-family:" + CssFontFamily() + ";color:" + CssWhite() + ";background:" + CssBackground() + ";border:" + BorderWidth() + " solid " + CssBorderColor() + ";padding:10px 14px;box-sizing:border-box}",
                ".label{color:" + CssGrayText() + ";font-size:" + FontSmall() + "px}",
                ".value{color:" + CssCyan() + ";font-size:" + FontNormal() + "px}",
                ".orange{color:" + CssOrange() + "}"
            });
            var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Exploration Overlay</title>"+
                       "<style>" + css + "</style></head><body>"+
                       "<div class='card'>"+
                       "<div><span class='label'>System:</span> <span id='sys' class='value'>...</span></div>"+
                       "<div><span class='label'>Bodies:</span> <span id='bod' class='value'>0</span> &nbsp; <span class='label'>Scanned:</span> <span id='sca' class='value'>0</span> &nbsp; <span class='label'>Mapped:</span> <span id='map' class='value'>0</span></div>"+
                       "<div><span class='label'>FSS:</span> <span id='fss' class='value'>0%</span></div>"+
                       "<div><span class='label'>Session:</span> <span id='sess' class='orange'>0 systems / 0 scans / 0 mapped</span></div>"+
                       "</div>"+
                       "<script>const g=(id)=>document.getElementById(id);function apply(s){ if(!s) return; const x=s.exploration; if(x){ g('sys').textContent=x.systemName||''; g('bod').textContent=x.totalBodies||0; g('sca').textContent=x.scannedBodies||0; g('map').textContent=x.mappedBodies||0; g('fss').textContent=((x.fssProgress||0)*100).toFixed(0)+'%'; } const ses=s.explorationSession; if(ses){ g('sess').textContent=(ses.systemsVisited||0)+' systems / '+(ses.totalScans||0)+' scans / '+(ses.totalMapped||0)+' mapped'; } }"+
                       "fetch('/api/state').then(r=>r.json()).then(apply);let ws; function connect(){ ws=new WebSocket((location.protocol=='https:'?'wss://':'ws://')+location.host+'/ws'); ws.onmessage=(e)=>{ try{apply(JSON.parse(e.data));}catch{} }; ws.onclose=()=>setTimeout(connect,1000);} connect();"+
                       "</script></body></html>";
            return html;
        }

        private static string CssFontFamily()
        {
            var n = AppConfiguration.OverlayFontName;
            if (string.IsNullOrWhiteSpace(n)) n = "Consolas";
            return $"'{n}', Consolas, monospace";
        }
        private static string CssColor(byte r, byte g, byte b) => $"rgb({r},{g},{b})";
        private static string CssWhite() => CssColor(220,220,220);
        private static string CssGrayText() => CssColor(160,160,160);
        private static string CssCyan() => CssColor(84,223,237);
        private static string CssOrange() => CssColor(AppConfiguration.OverlayTextColor.R, AppConfiguration.OverlayTextColor.G, AppConfiguration.OverlayTextColor.B);
        private static string CssBorderColor() => CssColor(AppConfiguration.OverlayBorderColor.R, AppConfiguration.OverlayBorderColor.G, AppConfiguration.OverlayBorderColor.B);
        private static string CssBackground()
        {
            var c = AppConfiguration.OverlayBackgroundColor;
            // Use dedicated web overlay opacity to decouple from desktop overlay
            var a = Math.Clamp(AppConfiguration.WebOverlayOpacity/100.0, 0.0, 1.0);
            return $"rgba({c.R},{c.G},{c.B},{a.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        }
        private static string BorderWidth() => "2px";
        private static string FontNormal() => AppConfiguration.OverlayFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        private static string FontSmall() => Math.Max(6, AppConfiguration.OverlayFontSize - 2).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private Task BroadcastAsync(object obj, CancellationToken token = default)
        {
            var json = JsonSerializer.Serialize(obj, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            var seg = new ArraySegment<byte>(bytes);
            var list = _sockets.Keys;
            foreach (var s in list)
            {
                if ( s.State == WebSocketState.Open)
                {
                    _ = s.SendAsync(seg, WebSocketMessageType.Text, true, token);
                }
            }
            return Task.CompletedTask;
        }

        private Task SendFullStateAsync(WebSocket socket, CancellationToken token)
        {
            var json = JsonSerializer.Serialize(_state, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        // Update methods called by app
        public void UpdateCommander(string name)
        { _state.Commander = name; _ = BroadcastAsync(_state); }
        public void UpdateShip(string ship)
        { _state.Ship = ship; _ = BroadcastAsync(_state); }
        public void UpdateBalance(long balance)
        { _state.Balance = balance; _ = BroadcastAsync(_state); }
        public void UpdateCargo(int count, int? capacity)
        { _state.CargoCount = count; _state.CargoCapacity = capacity; _ = BroadcastAsync(_state); }
        public void UpdateSession(long cargo, long credits)
        { _state.SessionCargo = cargo; _state.SessionCredits = credits; _ = BroadcastAsync(_state); }

        public void UpdateShipIconFromInternalName(string? internalShip)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(internalShip)) { _state.ShipIconUrl = string.Empty; _ = BroadcastAsync(_state); return; }
                var display = ShipIconService.GetShipDisplayName(internalShip);
                if (!string.IsNullOrWhiteSpace(display))
                {
                    _state.ShipIconUrl = "/images/ships/" + display + ".png";
                }
                else
                {
                    _state.ShipIconUrl = string.Empty;
                }
                _ = BroadcastAsync(_state);
            }
            catch { }
        }

        public void UpdateExploration(SystemExplorationData? data)
        { _state.Exploration = data; _ = BroadcastAsync(_state); }
        public void UpdateExplorationSession(ExplorationSessionData? data)
        { _state.ExplorationSession = data; _ = BroadcastAsync(_state); }

        public void UpdateCargoList(System.Collections.Generic.IEnumerable<CargoItem> items)
        {
            try
            {
                var list = new System.Collections.Generic.List<WebCargoItem>();
                foreach (var it in items)
                {
                    var name = string.IsNullOrEmpty(it.Localised) ? it.Name : it.Localised;
                    if (!string.IsNullOrEmpty(name))
                    {
                        name = char.ToUpperInvariant(name[0]) + name.Substring(1);
                    }
                    list.Add(new WebCargoItem { DisplayName = name ?? string.Empty, Count = it.Count });
                }
                // Sort by display name, similar to desktop UI
                list.Sort((a,b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                _state.Items = list;
                _ = BroadcastAsync(_state);
            }
            catch { }
        }

        public void UpdateCargoSize(string text)
        { _state.CargoBarText = text ?? string.Empty; _ = BroadcastAsync(_state); }

        public void Dispose() => Stop();
    }
}


