using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public static class NavRouteService
    {
        public sealed class RouteSummary
        {
            public List<JumpHop> Hops { get; } = new();
            public int? CurrentIndex { get; set; }
            public int Total { get; set; }
            public double? NextDistanceLy { get; set; }
            public double? RemainingLy { get; set; }
        }

        public static RouteSummary? TryReadSummary(string journalDir, string? currentSystemName, long? currentSystemAddress, int maxHops = 7)
        {
            try
            {
                var path = Path.Combine(journalDir, "NavRoute.json");
                if (!File.Exists(path)) return null;

                string json = ReadAllTextShared(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("Route", out var routeEl) || routeEl.ValueKind != JsonValueKind.Array)
                    return null;

                int count = routeEl.GetArrayLength();
                if (count == 0) return null;

                int currentIdx = -1;
                for (int i = 0; i < count; i++)
                {
                    var el = routeEl[i];
                    string? name = el.TryGetProperty("StarSystem", out var ns) ? ns.GetString() : null;
                    long? addr = null;
                    if (el.TryGetProperty("SystemAddress", out var sa) && sa.TryGetInt64(out var sav)) addr = sav;
                    if ((currentSystemAddress.HasValue && addr.HasValue && addr.Value == currentSystemAddress.Value) ||
                        (!string.IsNullOrEmpty(currentSystemName) && string.Equals(name, currentSystemName, StringComparison.OrdinalIgnoreCase)))
                    {
                        currentIdx = i;
                        break;
                    }
                }

                var summary = new RouteSummary { CurrentIndex = currentIdx, Total = count };

                // Compute hops starting from next up to maxHops
                int start = (currentIdx >= 0) ? currentIdx + 1 : 0;
                int end = Math.Min(count, start + maxHops);
                double accumulatedRemaining = 0;
                for (int i = start; i < count; i++)
                {
                    var hop = routeEl[i];
                    string name = hop.TryGetProperty("StarSystem", out var hn) ? (hn.GetString() ?? "") : "";
                    string? starClass = hop.TryGetProperty("StarClass", out var sc) ? sc.GetString() : null;
                    bool scoop = IsScoopable(starClass);
                    double? segDist = null;
                    if (i > 0)
                    {
                        var prev = routeEl[i - 1];
                        segDist = TryDistance(prev, hop);
                        if (segDist.HasValue) accumulatedRemaining += segDist.Value;
                    }
                    if (i < end)
                    {
                        summary.Hops.Add(new JumpHop { Name = name, StarClass = starClass, IsScoopable = scoop, DistanceLy = segDist });
                    }
                }

                // Next distance is the first hop segment from current to next (if known)
                if (currentIdx >= 0 && currentIdx + 1 < count)
                {
                    var cur = routeEl[currentIdx];
                    var nxt = routeEl[currentIdx + 1];
                    summary.NextDistanceLy = TryDistance(cur, nxt);
                }
                summary.RemainingLy = accumulatedRemaining > 0 ? accumulatedRemaining : null;

                return summary;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadAllTextShared(string path)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.UTF8);
                    return sr.ReadToEnd();
                }
                catch { System.Threading.Thread.Sleep(20); }
            }
            return string.Empty;
        }

        private static double? TryDistance(JsonElement a, JsonElement b)
        {
            try
            {
                if (a.TryGetProperty("StarPos", out var ap) && b.TryGetProperty("StarPos", out var bp) &&
                    ap.ValueKind == JsonValueKind.Array && bp.ValueKind == JsonValueKind.Array && ap.GetArrayLength() == 3 && bp.GetArrayLength() == 3)
                {
                    double ax = ap[0].GetDouble(); double ay = ap[1].GetDouble(); double az = ap[2].GetDouble();
                    double bx = bp[0].GetDouble(); double by = bp[1].GetDouble(); double bz = bp[2].GetDouble();
                    return Math.Sqrt(Math.Pow(bx - ax, 2) + Math.Pow(by - ay, 2) + Math.Pow(bz - az, 2));
                }
            }
            catch { }
            return null;
        }

        private static bool IsScoopable(string? starClass)
        {
            if (string.IsNullOrEmpty(starClass)) return false;
            char c = char.ToUpperInvariant(starClass[0]);
            return c == 'O' || c == 'B' || c == 'A' || c == 'F' || c == 'G' || c == 'K' || c == 'M';
        }
    }
}

