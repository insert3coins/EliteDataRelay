using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service for managing exploration data and session tracking.
    /// Uses async database operations for improved performance during rapid scan events.
    /// </summary>
    public class ExplorationDataService : IDisposable
    {
        private SystemExplorationData? _currentSystem;
        private ExplorationSessionData _sessionData = new ExplorationSessionData();
        private Dictionary<long, SystemExplorationData> _visitedSystems = new Dictionary<long, SystemExplorationData>();
        private readonly ExplorationDatabaseService _database;

        /// <summary>
        /// When true, suppresses SystemDataChanged and SessionDataChanged events.
        /// Useful for background batch imports to avoid cross-thread UI updates.
        /// </summary>
        public bool SuppressEvents { get; set; } = false;

        /// <summary>
        /// Event raised when the current system's exploration data changes.
        /// </summary>
        public event EventHandler<SystemExplorationData>? SystemDataChanged;

        /// <summary>
        /// Event raised when the session statistics change.
        /// </summary>
        public event EventHandler<ExplorationSessionData>? SessionDataChanged;

        public ExplorationDataService(ExplorationDatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        private void EmitSystemChanged()
        {
            if (!SuppressEvents && _currentSystem != null)
            {
                SystemDataChanged?.Invoke(this, _currentSystem);
            }
        }

        private void EmitSessionChanged()
        {
            if (!SuppressEvents)
            {
                SessionDataChanged?.Invoke(this, _sessionData);
            }
        }

        /// <summary>
        /// Gets the database service for direct access to cached data.
        /// </summary>
        public ExplorationDatabaseService Database => _database;

        /// <summary>
        /// Starts a new exploration session, resetting all statistics.
        /// </summary>
        public void StartSession()
        {
            _sessionData = new ExplorationSessionData
            {
                SessionStart = DateTime.Now
            };
            _visitedSystems.Clear();
            _currentSystem = null;

            Debug.WriteLine("[ExplorationDataService] Exploration session started");
            EmitSessionChanged();
        }

        /// <summary>
        /// Stops the current exploration session.
        /// </summary>
        public void StopSession()
        {
            Debug.WriteLine($"[ExplorationDataService] Exploration session ended. " +
                          $"Systems visited: {_sessionData.SystemsVisited}, " +
                          $"Total scans: {_sessionData.TotalScans}, " +
                          $"Total mapped: {_sessionData.TotalMapped}");
        }

        /// <summary>
        /// Handles a system change event (FSDJump, Location, CarrierJump).
        /// </summary>
        public void HandleSystemChange(string systemName, long? systemAddress, DateTime eventTimestamp)
        {
            if (!systemAddress.HasValue)
            {
                Debug.WriteLine("[ExplorationDataService] System change event without SystemAddress, skipping");
                return;
            }

            // Check if we've already visited this system in this session
            if (_visitedSystems.TryGetValue(systemAddress.Value, out var existingSystem))
            {
                _currentSystem = existingSystem;
                Debug.WriteLine($"[ExplorationDataService] Returned to previously visited system: {systemName}");
            }
            else
            {
                // Try to load from database cache
                _currentSystem = _database.LoadSystem(systemAddress.Value);

                if (_currentSystem == null)
                {
                    // New system not in cache - create and save it
                    _currentSystem = new SystemExplorationData
                    {
                        SystemName = systemName,
                        SystemAddress = systemAddress,
                        LastVisited = eventTimestamp,
                        LastUpdated = eventTimestamp,
                    };
                    Debug.WriteLine($"[ExplorationDataService] Entered new system: {systemName}");

                    // Save immediately so all visited systems are tracked
                    _database.SaveSystemAsync(_currentSystem);
                }
                else
                {
                    // Loaded from cache. Only update the LastVisited time if this event is newer.
                    if (eventTimestamp > _currentSystem.LastVisited)
                    {
                        _currentSystem.LastVisited = eventTimestamp;
                        _currentSystem.LastUpdated = eventTimestamp;
                        Debug.WriteLine($"[ExplorationDataService] Updating last visit time for {systemName} to {eventTimestamp:o}");
                        // Save the updated visit time to the database.
                        _database.SaveSystemAsync(_currentSystem);
                    }
                }

                _visitedSystems[systemAddress.Value] = _currentSystem;
                _sessionData.SystemsVisited++;
                EmitSessionChanged();
            }

            EmitSystemChanged();
        }

        /// <summary>
        /// Handles an FSS Discovery Scan event.
        /// </summary>
        public void HandleFSSDiscoveryScan(FSSDiscoveryScanEvent fssEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || fssEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] FSS scan event for different system, skipping");
                return;
            }

            _currentSystem.TotalBodies = fssEvent.BodyCount;
            _currentSystem.FSSProgress = fssEvent.Progress * 100;
            // Stop tracking system-level signals
            _currentSystem.NonBodySignalsDetected = 0;
            _currentSystem.SystemSignals.Clear();
            _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

            Debug.WriteLine($"[ExplorationDataService] FSS scan: {fssEvent.BodyCount} bodies, {fssEvent.Progress * 100:F1}% complete");

            // Save to database
            _database.SaveSystemAsync(_currentSystem);

            EmitSystemChanged();
        }

        /// <summary>
        /// Handles FSSAllBodiesFound event (explicit FSS completion and total bodies).
        /// </summary>
        public void HandleFSSAllBodiesFound(FSSAllBodiesFoundEvent evt, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || evt.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] FSSAllBodiesFound for different system, skipping");
                return;
            }

            _currentSystem.TotalBodies = Math.Max(_currentSystem.TotalBodies, evt.Count);
            _currentSystem.FSSProgress = 100;
            _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

            Debug.WriteLine($"[ExplorationDataService] FSSAllBodiesFound: {_currentSystem.TotalBodies} bodies");
            _database.SaveSystemAsync(_currentSystem);
            EmitSystemChanged();
        }

        /// <summary>
        /// Handles NavBeaconScan which provides NumBodies for the current system.
        /// Treat as equivalent to FSS completion for totals.
        /// </summary>
        public void HandleNavBeaconScan(NavBeaconScanEvent evt, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || evt.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] NavBeaconScan for different system, skipping");
                return;
            }

            _currentSystem.TotalBodies = Math.Max(_currentSystem.TotalBodies, evt.NumBodies);
            _currentSystem.FSSProgress = 100;
            _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

            Debug.WriteLine($"[ExplorationDataService] NavBeaconScan: {_currentSystem.TotalBodies} bodies");
            _database.SaveSystemAsync(_currentSystem);
            EmitSystemChanged();
        }

        /// <summary>
        /// Optionally handle FSSSignalDiscovered (non-body signals). Currently informational.
        /// </summary>
        public void HandleFSSSignalDiscovered(FSSSignalDiscoveredEvent evt)
        {
            // Signals tracking disabled by request
            return;
        }

        /// <summary>
        /// Handles legacy DiscoveryScan event.
        /// </summary>
        public void HandleDiscoveryScan(DiscoveryScanEvent evt, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null) return;
            if (evt.SystemAddress.HasValue && evt.SystemAddress != _currentSystem.SystemAddress) return;

            _currentSystem.TotalBodies = Math.Max(_currentSystem.TotalBodies, evt.Bodies);
            // DiscoveryScan implies the ping completed; treat like FSS complete for totals-only.
            _currentSystem.FSSProgress = Math.Max(_currentSystem.FSSProgress, 100);
            _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

            Debug.WriteLine($"[ExplorationDataService] DiscoveryScan: {_currentSystem.TotalBodies} bodies");
            _database.SaveSystemAsync(_currentSystem);
            EmitSystemChanged();
        }

        /// <summary>
        /// Handles FirstFootfall event (definitive).
        /// </summary>
        public void HandleFirstFootfall(FirstFootfallEvent evt, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || evt.SystemAddress != _currentSystem.SystemAddress) return;

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == evt.BodyID);
            if (body != null && !body.FirstFootfall)
            {
                body.FirstFootfall = true;
                _sessionData.FirstFootfalls++;
                _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;
                _database.SaveSystemAsync(_currentSystem);
                EmitSystemChanged();
                EmitSessionChanged();
            }
        }

        /// <summary>
        /// Handles ScanOrganic (exobiology) event.
        /// </summary>
        public void HandleScanOrganic(ScanOrganicEvent evt, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || evt.SystemAddress != _currentSystem.SystemAddress) return;

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == evt.BodyID);
            if (body == null && evt.BodyID.HasValue)
            {
                // Create a minimal placeholder body if needed
                body = new ScannedBody
                {
                    BodyID = evt.BodyID,
                    BodyName = evt.BodyName ?? $"Body {evt.BodyID}",
                    BodyType = "Unknown"
                };
                _currentSystem.Bodies.Add(body);
            }

            if (body != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(evt.Genus)) parts.Add(evt.Genus!);
                if (!string.IsNullOrWhiteSpace(evt.Species)) parts.Add(evt.Species!);
                if (!string.IsNullOrWhiteSpace(evt.Variant)) parts.Add(evt.Variant!);
                var label = string.Join(" ", parts);
                if (!string.IsNullOrWhiteSpace(label) && !body.BiologicalSignals.Contains(label))
                {
                    body.BiologicalSignals.Add(label);
                }

                _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;
                _database.SaveSystemAsync(_currentSystem);
                EmitSystemChanged();
            }
        }

        /// <summary>
        /// Handles SellOrganicData (Vista Genomics) event.
        /// </summary>
        public void HandleSellOrganicData(SellOrganicDataEvent evt)
        {
            _sessionData.SoldValue += evt.TotalEarnings;
            Debug.WriteLine($"[ExplorationDataService] Sold organic data for {evt.TotalEarnings:N0} CR");
            EmitSessionChanged();
        }

        /// <summary>
        /// Handles CodexEntry event (currently informational only for exploration UI).
        /// </summary>
        public void HandleCodexEntry(CodexEntryEvent evt)
        {
            // For now, we simply log biological codex entries.
            if (!string.IsNullOrEmpty(evt.Category) && evt.Category.Equals("Biological", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[ExplorationDataService] Codex biological entry: {evt.NameLocalised ?? evt.Name}");
                if (_currentSystem == null) return;
                var label = evt.NameLocalised ?? evt.Name;
                if (!string.IsNullOrWhiteSpace(label) && !_currentSystem.CodexBiologicalEntries.Contains(label, StringComparer.OrdinalIgnoreCase))
                {
                    _currentSystem.CodexBiologicalEntries.Add(label);
                    _currentSystem.LastUpdated = DateTime.UtcNow;
                    EmitSystemChanged();
                }
            }
        }

        /// <summary>
        /// Handles a Scan event (detailed scan of a body).
        /// </summary>
        public void HandleScan(ScanEvent scanEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || scanEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] Scan event for different system, skipping");
                return;
            }

            // Check if we already have this body
            var existingBody = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == scanEvent.BodyID);
            if (existingBody != null)
            {
                // Update existing body
                UpdateBodyFromScan(existingBody, scanEvent);
            }
            else
            {
                // Add new body
                var newBody = CreateBodyFromScan(scanEvent);
                _currentSystem.Bodies.Add(newBody);
                _currentSystem.ScannedBodies++;
                _sessionData.TotalScans++;

                if (!scanEvent.WasDiscovered)
                {
                    _sessionData.FirstDiscoveries++;
                }
            }

            _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

            Debug.WriteLine($"[ExplorationDataService] Scanned body: {scanEvent.BodyName}");

            // Save to database
            _database.SaveSystemAsync(_currentSystem);

            EmitSystemChanged();
            EmitSessionChanged();
        }

        /// <summary>
        /// Handles a SAA Scan Complete event (detailed surface mapping).
        /// </summary>
        public void HandleSAAScanComplete(SAAScanCompleteEvent saaEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || saaEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] SAA scan event for different system, skipping");
                return;
            }

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == saaEvent.BodyID);
            if (body != null)
            {
                if (!body.IsMapped)
                {
                    body.IsMapped = true;
                    body.ProbesUsed = saaEvent.ProbesUsed;
                    body.EfficiencyTarget = saaEvent.EfficiencyTarget;
                    _currentSystem.MappedBodies++;
                    _sessionData.TotalMapped++;

                    if (!body.WasMapped)
                    {
                        _sessionData.FirstMappings++;
                    }

                    Debug.WriteLine($"[ExplorationDataService] Mapped body: {saaEvent.BodyName} " +
                                  $"(Probes: {saaEvent.ProbesUsed}/{saaEvent.EfficiencyTarget})");
                    Debug.WriteLine($"[ExplorationDataService] System mapped count now: {_currentSystem.MappedBodies}, Session total mapped: {_sessionData.TotalMapped}");

                    _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

                    // Save to database
                    _database.SaveSystemAsync(_currentSystem);

                    Debug.WriteLine($"[ExplorationDataService] Firing SystemDataChanged event...");
                    EmitSystemChanged();
                    EmitSessionChanged();
                }
            }
        }

        /// <summary>
        /// Handles FSS Body Signals event.
        /// </summary>
        public void HandleFSSBodySignals(FSSBodySignalsEvent signalsEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || signalsEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                return;
            }

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == signalsEvent.BodyID);
            if (body != null)
            {
                body.Signals = signalsEvent.Signals;
                _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

                // Save to database
                _database.SaveSystemAsync(_currentSystem);

                EmitSystemChanged();
            }
        }

        /// <summary>
        /// Handles SAA Signals Found event (biological, geological signals during mapping).
        /// </summary>
        public void HandleSAASignalsFound(SAASignalsFoundEvent signalsEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || signalsEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                return;
            }

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == signalsEvent.BodyID);
            if (body != null)
            {
                body.Signals = signalsEvent.Signals;

                // Extract biological signals (genuses)
                if (signalsEvent.Genuses != null && signalsEvent.Genuses.Any())
                {
                    body.BiologicalSignals = signalsEvent.Genuses
                        .Select(g => g.NameLocalised ?? g.Name)
                        .ToList();

                    Debug.WriteLine($"[ExplorationDataService] Found biological signals on {signalsEvent.BodyName}: " +
                                  $"{string.Join(", ", body.BiologicalSignals)}");
                }

                _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

                // Save to database
                _database.SaveSystemAsync(_currentSystem);

                EmitSystemChanged();
            }
        }

        /// <summary>
        /// Handles selling exploration data.
        /// </summary>
        public void HandleSellExplorationData(SellExplorationDataEvent sellEvent, DateTime? eventTimestamp = null)
        {
            _sessionData.SoldValue += sellEvent.TotalEarnings;

            Debug.WriteLine($"[ExplorationDataService] Sold exploration data for {sellEvent.TotalEarnings:N0} CR " +
                          $"({sellEvent.Systems.Count} systems, {sellEvent.Discovered.Count} discoveries)");

            EmitSessionChanged();
        }

        /// <summary>
        /// Handles selling on-foot exploration data.
        /// </summary>
        public void HandleMultiSellExplorationData(MultiSellExplorationDataEvent sellEvent)
        {
            _sessionData.SoldValue += sellEvent.TotalEarnings;
            _sessionData.FirstFootfalls += sellEvent.FirstFootfallCount;

            Debug.WriteLine($"[ExplorationDataService] Sold on-foot data for {sellEvent.TotalEarnings:N0} CR " +
                          $"({sellEvent.FirstFootfallCount} first footfalls)");

            EmitSessionChanged();
        }

        /// <summary>
        /// Handles touchdown on a planet surface.
        /// </summary>
        public void HandleTouchdown(TouchdownEvent touchdownEvent, DateTime? eventTimestamp = null)
        {
            if (_currentSystem == null || touchdownEvent.SystemAddress != _currentSystem.SystemAddress)
            {
                Debug.WriteLine("[ExplorationDataService] Touchdown event for different system, skipping");
                return;
            }

            // Only count touchdowns on planets (not stations)
            if (touchdownEvent.OnPlanet != true)
            {
                return;
            }

            var body = _currentSystem.Bodies.FirstOrDefault(b => b.BodyID == touchdownEvent.BodyID);
            if (body != null)
            {
                // If the body was not previously discovered and is landable, this is a first footfall
                if (!body.WasDiscovered && body.Landable == true && !body.FirstFootfall)
                {
                    body.FirstFootfall = true;
                    _sessionData.FirstFootfalls++;

                    Debug.WriteLine($"[ExplorationDataService] First footfall on {touchdownEvent.Body}!");

                    _currentSystem.LastUpdated = eventTimestamp ?? DateTime.UtcNow;

                    // Save to database
                    _database.SaveSystemAsync(_currentSystem);

                    EmitSystemChanged();
                    EmitSessionChanged();
                }
            }
        }


        /// <summary>
        /// Resets all exploration data and session statistics.
        /// </summary>
        public void Reset()
        {
            _currentSystem = null;
            _sessionData = new ExplorationSessionData();
            _visitedSystems.Clear();

            Debug.WriteLine("[ExplorationDataService] All exploration data has been reset");
        }

        /// <summary>
        /// Gets the current system exploration data.
        /// </summary>
        public SystemExplorationData? GetCurrentSystemData()
        {
            return _currentSystem;
        }

        /// <summary>
        /// Gets the current session statistics.
        /// </summary>
        public ExplorationSessionData GetSessionData()
        {
            return _sessionData;
        }

        private ScannedBody CreateBodyFromScan(ScanEvent scanEvent)
        {
            return new ScannedBody
            {
                BodyName = scanEvent.BodyName,
                BodyID = scanEvent.BodyID,
                BodyType = scanEvent.StarType ?? scanEvent.PlanetClass ?? "Unknown",
                DistanceFromArrival = scanEvent.DistanceFromArrivalLS,
                Landable = scanEvent.Landable,
                WasDiscovered = scanEvent.WasDiscovered,
                WasMapped = scanEvent.WasMapped,
                TerraformState = scanEvent.TerraformState
            };
        }

        private void UpdateBodyFromScan(ScannedBody body, ScanEvent scanEvent)
        {
            // Update properties that might have changed
            body.BodyType = scanEvent.StarType ?? scanEvent.PlanetClass ?? body.BodyType;
            body.DistanceFromArrival = scanEvent.DistanceFromArrivalLS ?? body.DistanceFromArrival;
            body.Landable = scanEvent.Landable ?? body.Landable;
            body.TerraformState = scanEvent.TerraformState ?? body.TerraformState;
        }

        public void Dispose()
        {
            // Database is disposed by the main form
        }
    }
}
