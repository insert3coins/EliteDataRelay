using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// One-time importer that scans historical Elite Dangerous journal files
    /// to populate the exploration database for the Exploration tab.
    /// </summary>
    public class ExplorationHistoryImportService
    {
        private readonly ExplorationDataService _explorationDataService;

        public ExplorationHistoryImportService(ExplorationDataService explorationDataService)
        {
            _explorationDataService = explorationDataService ?? throw new ArgumentNullException(nameof(explorationDataService));
        }

        /// <summary>
        /// Runs the import if it hasn't been done yet. Returns true if an import was performed.
        /// </summary>
        public async Task<bool> ImportIfNeededAsync()
        {
            try
            {
                if (AppConfiguration.ExplorationHistoryImported)
                {
                    return false; // already done
                }

                var journalDir = AppConfiguration.JournalPath;
                if (string.IsNullOrWhiteSpace(journalDir) || !Directory.Exists(journalDir))
                {
                    Debug.WriteLine("[ExplorationHistoryImport] Journal directory not found; skipping import.");
                    AppConfiguration.ExplorationHistoryImported = true; // avoid retrying every launch
                    AppConfiguration.Save();
                    return false;
                }

                var files = Directory.EnumerateFiles(journalDir, "Journal.*.log")
                                      .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                      .ToList();
                if (files.Count == 0)
                {
                    Debug.WriteLine("[ExplorationHistoryImport] No journal files found; skipping import.");
                    AppConfiguration.ExplorationHistoryImported = true;
                    AppConfiguration.Save();
                    return false;
                }

                Debug.WriteLine($"[ExplorationHistoryImport] Starting historical import across {files.Count} files...");

                // Suppress UI events and use synchronous DB writes during import
                _explorationDataService.SuppressEvents = true;
                _explorationDataService.Database.StopBackgroundWriter();

                try
                {
                    // Process on a background thread to avoid blocking UI
                    await Task.Run(() => ProcessFiles(files));
                }
                finally
                {
                    // Restore normal operation
                    _explorationDataService.Database.StartBackgroundWriter();
                    _explorationDataService.SuppressEvents = false;
                }

                AppConfiguration.ExplorationHistoryImported = true;
                AppConfiguration.Save();

                Debug.WriteLine("[ExplorationHistoryImport] Historical import completed.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExplorationHistoryImport] Failed during import: {ex}");
                // Do not set the flag on failure so we can try again next launch.
                return false;
            }
        }

        private void ProcessFiles(List<string> files)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var file in files)
            {
                try
                {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(line);
                            var root = jsonDoc.RootElement;

                            if (!root.TryGetProperty("event", out var evtProp)) continue;
                            var evt = evtProp.GetString();

                            // Timestamp for ordering/visit times
                            DateTime timestamp = DateTime.UtcNow;
                            if (root.TryGetProperty("timestamp", out var tsElem) && tsElem.TryGetDateTime(out var parsedTs))
                            {
                                timestamp = parsedTs;
                            }

                            switch (evt)
                            {
                                // System context events
                                case "FSDJump":
                                case "Location":
                                case "CarrierJump":
                                    {
                                        string? systemName = root.TryGetProperty("StarSystem", out var s) ? s.GetString() : null;
                                        long? systemAddress = root.TryGetProperty("SystemAddress", out var a) && a.ValueKind == JsonValueKind.Number
                                            ? a.GetInt64()
                                            : (long?)null;
                                        if (!string.IsNullOrEmpty(systemName) && systemAddress.HasValue)
                                        {
                                            _explorationDataService.HandleSystemChange(systemName!, systemAddress, timestamp);
                                        }
                                        break;
                                    }

                                // Exploration events
                                case "FSSDiscoveryScan":
                                    {
                                        var e = JsonSerializer.Deserialize<FSSDiscoveryScanEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleFSSDiscoveryScan(e, timestamp);
                                        break;
                                    }
                                case "FSSAllBodiesFound":
                                    {
                                        var e = JsonSerializer.Deserialize<FSSAllBodiesFoundEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleFSSAllBodiesFound(e, timestamp);
                                        break;
                                    }
                                case "DiscoveryScan":
                                    {
                                        var e = JsonSerializer.Deserialize<DiscoveryScanEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleDiscoveryScan(e, timestamp);
                                        break;
                                    }
                                case "NavBeaconScan":
                                    {
                                        var e = JsonSerializer.Deserialize<NavBeaconScanEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleNavBeaconScan(e, timestamp);
                                        break;
                                    }
                                case "Scan":
                                    {
                                        var e = JsonSerializer.Deserialize<ScanEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleScan(e, timestamp);
                                        break;
                                    }
                                case "SAAScanComplete":
                                    {
                                        var e = JsonSerializer.Deserialize<SAAScanCompleteEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleSAAScanComplete(e, timestamp);
                                        break;
                                    }
                                case "FSSBodySignals":
                                    {
                                        var e = JsonSerializer.Deserialize<FSSBodySignalsEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleFSSBodySignals(e, timestamp);
                                        break;
                                    }
                                case "SAASignalsFound":
                                    {
                                        var e = JsonSerializer.Deserialize<SAASignalsFoundEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleSAASignalsFound(e, timestamp);
                                        break;
                                    }
                                case "FirstFootfall":
                                    {
                                        var e = JsonSerializer.Deserialize<FirstFootfallEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleFirstFootfall(e, timestamp);
                                        break;
                                    }
                                case "ScanOrganic":
                                    {
                                        var e = JsonSerializer.Deserialize<ScanOrganicEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleScanOrganic(e, timestamp);
                                        break;
                                    }
                                case "CodexEntry":
                                    {
                                        var e = JsonSerializer.Deserialize<CodexEntryEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleCodexEntry(e);
                                        break;
                                    }
                                case "FSSSignalDiscovered":
                                    {
                                        var e = JsonSerializer.Deserialize<FSSSignalDiscoveredEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleFSSSignalDiscovered(e);
                                        break;
                                    }
                                case "SellExplorationData":
                                    {
                                        var e = JsonSerializer.Deserialize<SellExplorationDataEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleSellExplorationData(e, timestamp);
                                        break;
                                    }
                                case "SellOrganicData":
                                    {
                                        var e = JsonSerializer.Deserialize<SellOrganicDataEvent>(line, options);
                                        if (e != null) _explorationDataService.HandleSellOrganicData(e);
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip malformed lines; historical files sometimes contain partial lines
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExplorationHistoryImport] Error processing file '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }
    }
}
