using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;


namespace Sem2Proj.Models
{
    public class AssetManager
    {
        private readonly string _dbPath = "Data Source=Data/heat_optimization.db;Version=3;";

        public HeatingGrid? GridInfo { get; private set; }
        public List<AssetModel> AllAssets { get; private set; } = new();
        public List<AssetModel> CurrentAssets { get; private set; } = new();
        public List<Preset> Presets { get; private set; } = new();
        
        public string CurrentScenarioName => Presets.Count > 0 && SelectedScenarioIndex >= 0
            ? Presets[SelectedScenarioIndex].Name
            : "No Scenario";

        public int SelectedScenarioIndex { get; private set; } = -1;

        public AssetManager()
        {
            LoadAssetsAndPresetsFromDatabase();
            
            if (Presets.Count > 0)
            {
                SetScenario(0);
            }
        }

        private void LoadAssetsAndPresetsFromDatabase()
        {
            try
            {
                using (var conn = new SQLiteConnection(_dbPath))
                {
                    conn.Open();
                    
                    // Load all assets
                    LoadAllAssets(conn);
                    
                    // Load all presets
                    LoadAllPresets(conn);
                    
                    // Load heating grid info
                    LoadHeatingGridInfo(conn);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database error in AssetManager: {ex.Message}");
            }
        }

        private void LoadAllAssets(SQLiteConnection conn)
        {
            const string query = "SELECT * FROM AM_Assets";
            using (var cmd = new SQLiteCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    AllAssets.Add(new AssetModel
                    {
                        Name = reader["Name"].ToString() ?? string.Empty,
                        ImageSource = reader["ImageSource"].ToString() ?? string.Empty,
                        MaxHeat = Convert.ToDouble(reader["MaxHeat"]),
                        ProductionCosts = Convert.ToDouble(reader["ProductionCosts"]),
                        Emissions = Convert.ToDouble(reader["Emissions"]),
                        GasConsumption = Convert.ToDouble(reader["GasConsumption"]),
                        OilConsumption = Convert.ToDouble(reader["OilConsumption"]),
                        MaxElectricity = Convert.ToDouble(reader["MaxElectricity"])
                    });
                }
            }
            Debug.WriteLine($"Loaded {AllAssets.Count} assets from database");
        }

        private void LoadAllPresets(SQLiteConnection conn)
        {
            // First load preset definitions
            const string presetQuery = "SELECT * FROM AM_Presets";
            using (var presetCmd = new SQLiteCommand(presetQuery, conn))
            using (var presetReader = presetCmd.ExecuteReader())
            {
                while (presetReader.Read())
                {
                    var preset = new Preset
                    {
                        Id = Convert.ToInt32(presetReader["Id"]),
                        Name = presetReader["Name"].ToString() ?? string.Empty
                    };
                    Presets.Add(preset);
                }
            }

            // Then load asset associations for each preset
            foreach (var preset in Presets)
            {
                const string assetQuery = @"
                    SELECT a.* 
                    FROM AM_Assets a
                    JOIN AM_PresetAssets pa ON a.Id = pa.AssetId
                    WHERE pa.PresetId = @presetId";
                
                using (var assetCmd = new SQLiteCommand(assetQuery, conn))
                {
                    assetCmd.Parameters.AddWithValue("@presetId", preset.Id);
                    using (var assetReader = assetCmd.ExecuteReader())
                    {
                        while (assetReader.Read())
                        {
                            var assetName = assetReader["Name"].ToString() ?? string.Empty;
                            preset.Machines.Add(assetName);
                        }
                    }
                }
            }
            Debug.WriteLine($"Loaded {Presets.Count} presets from database");
        }

        private void LoadHeatingGridInfo(SQLiteConnection conn)
        {
            const string query = "SELECT * FROM AM_HeatingGrid LIMIT 1";
            using (var cmd = new SQLiteCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    GridInfo = new HeatingGrid
                    {
                        Name = reader["Name"].ToString() ?? string.Empty,
                        ImageSource = reader["ImageSource"].ToString() ?? string.Empty,
                        Architecture = reader["Architecture"].ToString() ?? string.Empty,
                        Size = reader["Size"].ToString() ?? string.Empty
                    };
                    Debug.WriteLine($"Loaded heating grid: {GridInfo.Name}");
                }
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
            CurrentAssets = AllAssets
                .Where(a => preset.Machines.Contains(a.Name))
                .ToList();

            SelectedScenarioIndex = scenarioIndex;
            Debug.WriteLine($"Set scenario '{preset.Name}' with {CurrentAssets.Count} assets");
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
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Machines { get; set; } = new();
    }

    public partial class AssetModel : ObservableObject
    {
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string imageSource = string.Empty;
          [ObservableProperty] private Bitmap? _imageFromBinding;
        [ObservableProperty] private double maxHeat;
        [ObservableProperty] private double productionCosts;
        [ObservableProperty] private double emissions;
        [ObservableProperty] private double gasConsumption;
        [ObservableProperty] private double oilConsumption;
        [ObservableProperty] private double maxElectricity;

        public bool IsElectricBoiler => MaxElectricity < 0;
        public bool IsGenerator => MaxElectricity > 0;
        public double CostPerMW => ProductionCosts;
        public double EmissionsPerMW => MaxHeat > 0 ? Emissions / MaxHeat : 0;
    }

    public partial class HeatingGrid : ObservableObject
    {
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _imageSource = string.Empty;
        [ObservableProperty] private string _architecture = string.Empty;
        [ObservableProperty] private string _size = string.Empty;
    }
}