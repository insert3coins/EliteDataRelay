using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// A static class responsible for generating HTML reports from mining session data.
    /// </summary>
    public static class ReportGenerator
    {
        /// <summary>
        /// Generates a self-contained HTML report for a given collection of mining sessions.
        /// </summary>
        /// <param name="sessions">The mining session records to include in the report.</param>
        /// <param name="title">An optional title for the report.</param>
        /// <returns>A string containing the full HTML document.</returns>
        public static string GenerateHtmlReport(IEnumerable<MiningSessionRecord> sessions, string? title = null)
        {
            var data = sessions.Select(r => r.Clone()).ToList();

            if (data.Count == 0)
            {
                return "<html><body><h1>No mining sessions recorded.</h1></body></html>";
            }

            title ??= "Elite Data Relay – Mining Session Report";
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
            sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',Tahoma,sans-serif;background:#040404;color:#eee;margin:0;padding:24px;}");
            sb.AppendLine("h1{color:#ff8800;margin-bottom:8px;} h2{color:#f0f0f0;} table{border-collapse:collapse;width:100%;margin-bottom:24px;} th,td{border:1px solid #222;padding:8px;text-align:left;} th{background:#111;color:#ff8800;} tr:nth-child(even){background:#0d0d0d;} .card{background:#111;border:1px solid #222;border-radius:6px;padding:16px;margin-bottom:24px;}");
            sb.AppendLine("canvas{max-width:100%;}");
            sb.AppendLine(".metrics{display:flex;gap:16px;flex-wrap:wrap;} .metric{flex:1 1 200px;background:#121212;border-radius:6px;padding:12px;border:1px solid #1f1f1f;}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
            sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");

            sb.AppendLine("<div class=\"card\"><h2>Session Overview</h2><div class=\"metrics\">");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Sessions</strong><br/>{data.Count}</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Credits</strong><br/>{data.Sum(r => r.CreditsEarned):N0} cr</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Refined</strong><br/>{data.Sum(r => r.RefinedCommodities.Values.Sum()):N0} units</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Limpets Used</strong><br/>{data.Sum(r => r.LimpetsUsed):N0}</div>");
            sb.AppendLine("</div></div>");

            sb.AppendLine("<div class=\"card\"><canvas id=\"creditsChart\"></canvas></div>");

            sb.AppendLine("<table><thead><tr><th>Start</th><th>End</th><th>Duration</th><th>Mining Time</th><th>Credits</th><th>Cargo</th><th>Limpets</th><th>Final Fill %</th></tr></thead><tbody>");
            foreach (var record in data)
            {
                sb.AppendLine($"<tr><td>{record.SessionStart:yyyy-MM-dd HH:mm}</td><td>{record.SessionEnd:yyyy-MM-dd HH:mm}</td><td>{record.SessionDuration}</td><td>{record.MiningDuration}</td><td>{record.CreditsEarned:N0}</td><td>{record.TotalCargoCollected:N0}</td><td>{record.LimpetsUsed}</td><td>{record.FinalCargoFillPercent:F1}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            var refinedTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in data)
            {
                foreach (var kvp in record.RefinedCommodities)
                {
                    if (refinedTotals.ContainsKey(kvp.Key)) refinedTotals[kvp.Key] += kvp.Value;
                    else refinedTotals[kvp.Key] = kvp.Value;
                }
            }

            if (refinedTotals.Count > 0)
            {
                sb.AppendLine("<div class=\"card\"><h2>Refined Commodities</h2><table><thead><tr><th>Commodity</th><th>Total Refined</th></tr></thead><tbody>");
                foreach (var kvp in refinedTotals.OrderByDescending(k => k.Value))
                {
                    sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(kvp.Key)}</td><td>{kvp.Value:N0}</td></tr>");
                }
                sb.AppendLine("</tbody></table></div>");
            }

            var labels = string.Join(',', data.Select(r => $"'{r.SessionStart:MM-dd HH:mm}'"));
            var credits = string.Join(',', data.Select(r => r.CreditsEarned));
            var cargo = string.Join(',', data.Select(r => r.TotalCargoCollected));
            sb.AppendLine("<script>");
            sb.AppendLine("const ctx=document.getElementById('creditsChart').getContext('2d');");
            sb.AppendLine("new Chart(ctx,{type:'bar',data:{labels:[" + labels + "],datasets:[{label:'Credits Earned',data:[" + credits + "],backgroundColor:'rgba(255,136,0,0.6)',borderColor:'rgba(255,136,0,1)',borderWidth:1},{label:'Cargo Collected',data:[" + cargo + "],backgroundColor:'rgba(0,180,255,0.5)',borderColor:'rgba(0,180,255,1)',borderWidth:1}]},options:{responsive:true,plugins:{tooltip:{callbacks:{label:function(context){return context.dataset.label+': '+context.parsed.y.toLocaleString();}}}},scales:{y:{ticks:{color:'#ccc'},grid:{color:'#222'}},x:{ticks:{color:'#ccc'},grid:{color:'#222'}}}}});");
            sb.AppendLine("</script>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}