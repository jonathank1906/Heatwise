using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sem2Proj.Models
{
    public class AssetManager
    {
        public List<AssetModel> AllAssets { get; private set; } = new();
        public List<AssetModel> CurrentAssets { get; private set; } = new();
        public List<Preset> Presets { get; private set; } = new();
        public string CurrentScenarioName => Presets.Count > 0 && SelectedScenarioIndex >= 0 
            ? Presets[SelectedScenarioIndex].Name 
            : "No Scenario";

        public int SelectedScenarioIndex { get; private set; } = -1;

        public AssetManager()
        {
            LoadAssetsAndPresetsFromJson();
            
            // Set default scenario if any exist
            if (Presets.Count > 0)
            {
                SetScenario(0);
            }
        }

        public void LoadAssetsAndPresetsFromJson()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string jsonFilePath = Path.Combine(basePath, "Data/HeatProductionUnits.json");

                if (!File.Exists(jsonFilePath))
                {
                    Debug.WriteLine($"Asset configuration file not found at: {jsonFilePath}");
                    return;
                }

                string jsonString = File.ReadAllText(jsonFilePath);
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                // Load all assets
                if (root.TryGetProperty("Assets", out var assetsElement))
                {
                    AllAssets = JsonSerializer.Deserialize<List<AssetModel>>(assetsElement.GetRawText()) 
                        ?? new List<AssetModel>();
                    Debug.WriteLine($"Loaded {AllAssets.Count} total assets.");
                }

                // Load all presets/scenarios
                if (root.TryGetProperty("Presets", out var presetsElement))
                {
                    Presets = JsonSerializer.Deserialize<List<Preset>>(presetsElement.GetRawText()) 
                        ?? new List<Preset>();
                    Debug.WriteLine($"Loaded {Presets.Count} presets/scenarios.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading asset configuration: {ex.Message}");
            }
        }

        public bool SetScenario(int scenarioIndex)
        {
            if (scenarioIndex < 0 || scenarioIndex >= Presets.Count)
            {
                Debug.WriteLine($"Invalid scenario index: {scenarioIndex}");
                return false;
            }

            var preset = Presets[scenarioIndex];
            var newAssets = new List<AssetModel>();

            foreach (var machineName in preset.Machines)
            {
                var asset = AllAssets.FirstOrDefault(a => a.Name == machineName);
                if (asset != null)
                {
                    newAssets.Add(asset);
                }
                else
                {
                    Debug.WriteLine($"Warning: Machine '{machineName}' not found in assets");
                }
            }

            CurrentAssets = newAssets;
            SelectedScenarioIndex = scenarioIndex;

            Debug.WriteLine($"Scenario set to '{preset.Name}' with {CurrentAssets.Count} assets:");
            foreach (var asset in CurrentAssets)
            {
                Debug.WriteLine($"- {asset.Name} (Max Heat: {asset.MaxHeat} MW)");
            }

            return true;
        }

        public bool SetScenario(string scenarioName)
        {
            var preset = Presets.FirstOrDefault(p => p.Name.Equals(scenarioName, StringComparison.OrdinalIgnoreCase));
            if (preset == null)
            {
                Debug.WriteLine($"Scenario '{scenarioName}' not found");
                return false;
            }

            return SetScenario(Presets.IndexOf(preset));
        }

        public AssetModel? GetAssetByName(string name)
        {
            return AllAssets.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class Preset
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Machines { get; set; } = new();
    }

    public partial class AssetModel : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string imageSource = string.Empty;

        [ObservableProperty]
        private double maxHeat;

        [ObservableProperty]
        private double productionCosts;

        [ObservableProperty]
        private double emissions;

        [ObservableProperty]
        private double gasConsumption;

        [ObservableProperty]
        private double oilConsumption;

        [ObservableProperty]
        private double maxElectricity;

        // Helper properties
        public bool IsElectricBoiler => MaxElectricity < 0; // Consumes electricity
        public bool IsGenerator => MaxElectricity > 0; // Produces electricity
        public double CostPerMW => MaxHeat > 0 ? ProductionCosts / MaxHeat : 0;
        public double EmissionsPerMW => MaxHeat > 0 ? Emissions / MaxHeat : 0;
    }
}