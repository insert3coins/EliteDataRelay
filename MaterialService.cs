using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.Services
{
    public class MaterialService : IMaterialService, IDisposable
    {
        private readonly IJournalWatcherService _journalWatcher;
        private readonly string _liveMaterialsFilePath = Path.Combine(AppConfiguration.JournalPath, "Materials.json");
        private readonly string _inventoryFilePath = Path.Combine(AppConfiguration.JournalPath, "MaterialInventory.json");
        private readonly Dictionary<string, MaterialItem> _raw = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, MaterialItem> _manufactured = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, MaterialItem> _encoded = new(StringComparer.InvariantCultureIgnoreCase);

        public event EventHandler? MaterialsUpdated;

        public IReadOnlyDictionary<string, MaterialItem> RawMaterials => _raw;
        public IReadOnlyDictionary<string, MaterialItem> ManufacturedMaterials => _manufactured;
        public IReadOnlyDictionary<string, MaterialItem> EncodedMaterials => _encoded;

        public MaterialService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher;
        }

        public void Start()
        {
            // Load the initial state from the snapshot file first. This gives us a baseline
            // even if the app is started long after the game session began.
            LoadInitialStateFromFile();

            _journalWatcher.MaterialsEvent += OnMaterials;
            _journalWatcher.MaterialCollectedEvent += OnMaterialCollected;
            _journalWatcher.MaterialDiscardedEvent += OnMaterialDiscarded;
            _journalWatcher.MaterialTradeEvent += OnMaterialTrade;
            _journalWatcher.EngineerCraftEvent += OnEngineerCraft;
        }

        public void Stop()
        {
            _journalWatcher.MaterialsEvent -= OnMaterials;
            _journalWatcher.MaterialCollectedEvent -= OnMaterialCollected;
            _journalWatcher.MaterialDiscardedEvent -= OnMaterialDiscarded;
            _journalWatcher.MaterialTradeEvent -= OnMaterialTrade;
            _journalWatcher.EngineerCraftEvent -= OnEngineerCraft;
        }

        private void LoadInitialStateFromFile()
        {
            // Based on reports, Materials.json is no longer reliably created.
            // We will use MaterialInventory.json as a fallback if the journal scan
            // doesn't find a `Materials` event (e.g. for very long sessions
            // spanning multiple journal files).
            if (TryLoadMaterialsFile(_inventoryFilePath))
            {
                Debug.WriteLine($"[MaterialService] Loaded initial state from backup MaterialInventory.json");
                return;
            }

            Debug.WriteLine($"[MaterialService] No material snapshot files found. Waiting for journal event.");
        }

        private bool TryLoadMaterialsFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return false;

                // The structure of MaterialInventory.json is the same as the "Materials" event payload.
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var snapshot = JsonSerializer.Deserialize<MaterialsEvent>(json, options);

                if (snapshot != null)
                {
                    _raw.Clear();
                    _manufactured.Clear();
                    _encoded.Clear();

                    snapshot.Raw.ForEach(m => _raw[m.Name] = m);
                    snapshot.Manufactured.ForEach(m => _manufactured[m.Name] = m);
                    snapshot.Encoded.ForEach(m => _encoded[m.Name] = m);

                    MaterialsUpdated?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MaterialService] Error reading material file {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return false;
        }

        private void OnMaterials(object? sender, MaterialsEventArgs e)
        {
            Debug.WriteLine("[MaterialService] Processing full materials snapshot.");
            _raw.Clear();
            _manufactured.Clear();
            _encoded.Clear();

            e.EventData.Raw.ForEach(m => _raw[m.Name] = m);
            e.EventData.Manufactured.ForEach(m => _manufactured[m.Name] = m);
            e.EventData.Encoded.ForEach(m => _encoded[m.Name] = m);

            MaterialsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMaterialCollected(object? sender, MaterialCollectedEventArgs e)
        {
            var categoryDict = GetCategoryDictionary(e.EventData.Category);
            if (categoryDict == null) return;

            if (categoryDict.TryGetValue(e.EventData.Name, out var material))
            {
                material.Count += e.EventData.Count;
            }
            else
            {
                // This can happen if a material is collected for the first time
                // and we haven't received a full 'Materials' snapshot yet.
                categoryDict[e.EventData.Name] = new MaterialItem { Name = e.EventData.Name, Count = e.EventData.Count };
            }

            Debug.WriteLine($"[MaterialService] Collected {e.EventData.Count} of {e.EventData.Name}. New total: {categoryDict[e.EventData.Name].Count}");
            MaterialsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMaterialDiscarded(object? sender, MaterialCollectedEventArgs e) // Using MaterialCollectedEventArgs as structure is identical
        {
            var categoryDict = GetCategoryDictionary(e.EventData.Category);
            if (categoryDict == null) return;

            if (categoryDict.TryGetValue(e.EventData.Name, out var material))
            {
                material.Count -= e.EventData.Count;
                if (material.Count < 0) material.Count = 0;
                Debug.WriteLine($"[MaterialService] Discarded {e.EventData.Count} of {e.EventData.Name}. New total: {material.Count}");
                MaterialsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnMaterialTrade(object? sender, MaterialTradeEventArgs e)
        {
            // Decrement paid material
            var paidCategory = GetCategoryForMaterial(e.EventData.Paid.Name);
            var paidDict = GetCategoryDictionary(paidCategory);
            if (paidDict != null && paidDict.TryGetValue(e.EventData.Paid.Name, out var paidMaterial))
            {
                paidMaterial.Count -= e.EventData.Paid.Count;
            }

            // Increment received material
            var receivedCategory = GetCategoryForMaterial(e.EventData.Received.Name);
            var receivedDict = GetCategoryDictionary(receivedCategory);
            if (receivedDict != null)
            {
                if (receivedDict.TryGetValue(e.EventData.Received.Name, out var receivedMaterial))
                {
                    receivedMaterial.Count += e.EventData.Received.Count;
                }
                else
                {
                    receivedDict[e.EventData.Received.Name] = e.EventData.Received;
                }
            }
            Debug.WriteLine($"[MaterialService] Traded materials.");
            MaterialsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnEngineerCraft(object? sender, EngineerCraftEventArgs e)
        {
            foreach (var consumed in e.EventData.Materials)
            {
                var category = GetCategoryForMaterial(consumed.Name);
                var dict = GetCategoryDictionary(category);
                if (dict != null && dict.TryGetValue(consumed.Name, out var material))
                {
                    material.Count -= consumed.Count;
                }
            }
            Debug.WriteLine($"[MaterialService] Materials consumed for crafting.");
            MaterialsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private Dictionary<string, MaterialItem>? GetCategoryDictionary(string category) =>
            category.ToLower() switch
            {
                "raw" => _raw,
                "manufactured" => _manufactured,
                "encoded" => _encoded,
                _ => null
            };

        // This is a helper for events that don't provide a category. It's not perfectly efficient.
        private string GetCategoryForMaterial(string name)
        {
            if (_raw.ContainsKey(name)) return "Raw";
            if (_manufactured.ContainsKey(name)) return "Manufactured";
            if (_encoded.ContainsKey(name)) return "Encoded";
            return string.Empty;
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}