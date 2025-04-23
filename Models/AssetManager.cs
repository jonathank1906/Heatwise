using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using System.Windows.Input;


namespace Sem2Proj.Models;

public class AssetManager
{
public event Action? AssetsChanged;
     private readonly Dictionary<int, List<AssetModel>> _scenarioAssets = new();
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
        const string query = "SELECT * FROM AM_Assets ORDER BY Id ASC";
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
            return false;

        // Check if we already have fresh data for this scenario
        if (!_scenarioAssets.TryGetValue(scenarioIndex, out var assets))
        {
            // Load fresh from database if not cached
            assets = LoadAssetsForScenario(scenarioIndex);
            _scenarioAssets[scenarioIndex] = assets;
        }

        CurrentAssets = assets;
        SelectedScenarioIndex = scenarioIndex;
        Debug.WriteLine($"Set scenario '{Presets[scenarioIndex].Name}' with {CurrentAssets.Count} assets");
        return true;
    }

 private List<AssetModel> LoadAssetsForScenario(int scenarioIndex)
    {
        var preset = Presets[scenarioIndex];
        var assets = new List<AssetModel>();

        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();
            const string query = @"
                SELECT a.* 
                FROM AM_Assets a
                JOIN AM_PresetAssets pa ON a.Id = pa.AssetId
                WHERE pa.PresetId = @presetId";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@presetId", preset.Id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        assets.Add(new AssetModel
                        {
                            Name = reader["Name"].ToString() ?? string.Empty,
                            // ... other properties ...
                        });
                    }
                }
            }
        }
        return assets;
    }

    public void RefreshAllScenarios()
    {
        _scenarioAssets.Clear();
        if (SelectedScenarioIndex >= 0)
        {
            SetScenario(SelectedScenarioIndex); // This will force a fresh load
        }
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

    public bool CreateNewAsset(string name, string imagePath, double maxHeat, double maxElectricity,
                             double productionCost, double emissions, double gasConsumption,
                             double oilConsumption, string? presetName = null)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Get the current maximum ID
                int newId = 1;
                const string getMaxIdQuery = "SELECT MAX(Id) FROM AM_Assets";
                using (var cmd = new SQLiteCommand(getMaxIdQuery, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        newId = Convert.ToInt32(result) + 1;
                    }
                }

                // Insert new asset with explicit ID
                const string insertAssetQuery = @"
            INSERT INTO AM_Assets 
            (Id, Name, ImageSource, MaxHeat, MaxElectricity, ProductionCosts, Emissions, GasConsumption, OilConsumption)
            VALUES
            (@id, @name, @imageSource, @maxHeat, @maxElectricity, @productionCosts, @emissions, @gasConsumption, @oilConsumption)";

                using (var cmd = new SQLiteCommand(insertAssetQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@imageSource", imagePath ?? string.Empty);
                    cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                    cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                    cmd.Parameters.AddWithValue("@productionCosts", productionCost);
                    cmd.Parameters.AddWithValue("@emissions", emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);

                    cmd.ExecuteNonQuery();
                }

                Debug.WriteLine($"New asset created with ID: {newId}");

                // Only add to the selected preset if specified
                if (!string.IsNullOrEmpty(presetName))
                {
                    // Find the preset ID
                    const string getPresetIdQuery = "SELECT Id FROM AM_Presets WHERE Name = @presetName";
                    int presetId = 0;

                    using (var cmd = new SQLiteCommand(getPresetIdQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@presetName", presetName);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            presetId = Convert.ToInt32(result);
                        }
                    }

                    if (presetId > 0)
                    {
                        const string addToPresetQuery = @"
                    INSERT INTO AM_PresetAssets (PresetId, AssetId)
                    VALUES (@presetId, @assetId)";

                        using (var cmd = new SQLiteCommand(addToPresetQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@presetId", presetId);
                            cmd.Parameters.AddWithValue("@assetId", newId);
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine($"Added asset ONLY to preset {presetName} (ID: {presetId})");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Preset {presetName} not found");
                    }
                }

                // Refresh the local collections
                AssetsChanged?.Invoke();
                LoadAssetsAndPresetsFromDatabase();
               
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating asset: {ex.Message}");
            return false;
        }
    }

    public void RefreshAssets()
    {
        // Clear existing collections
        AllAssets.Clear();
        CurrentAssets.Clear();
        Presets.Clear();

        // Reload from database
        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();
            LoadAllAssets(conn);
            LoadAllPresets(conn);
        }

        // Reapply current scenario if one was selected
        if (SelectedScenarioIndex >= 0)
        {
            SetScenario(SelectedScenarioIndex);
        }
    }

    public bool RemoveMachineFromPreset(string presetName, string machineName)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Get the preset ID
                var preset = Presets.FirstOrDefault(p => p.Name == presetName);
                if (preset == null) return false;

                // Get the asset ID
                var asset = AllAssets.FirstOrDefault(a => a.Name == machineName);
                if (asset == null) return false;

                // Find the asset ID in the database (since our in-memory model might not have IDs)
                int assetId;
                const string getAssetIdQuery = "SELECT Id FROM AM_Assets WHERE Name = @name";
                using (var cmd = new SQLiteCommand(getAssetIdQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@name", machineName);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return false;
                    assetId = Convert.ToInt32(result);
                }

                // Remove from the preset
                const string deleteQuery = "DELETE FROM AM_PresetAssets WHERE PresetId = @presetId AND AssetId = @assetId";
                using (var cmd = new SQLiteCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", preset.Id);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update the in-memory model
                        preset.Machines.Remove(machineName);
                        RefreshAssets();
                        return true;
                    }
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing machine from preset: {ex.Message}");
            return false;
        }
    }
     public void RefreshAllData()
    {
        // Clear all cached data
        AllAssets.Clear();
        CurrentAssets.Clear();
        Presets.Clear();
        
        // Reload everything from database
        LoadAssetsAndPresetsFromDatabase();
        
        // Reapply current scenario if one was selected
        if (SelectedScenarioIndex >= 0)
        {
            // Force a complete refresh of the current scenario
            var currentScenarioName = Presets[SelectedScenarioIndex].Name;
            SetScenario(currentScenarioName);
        }
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
    [ObservableProperty]
    private ICommand? deleteCommand;

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