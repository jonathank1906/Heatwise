using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using System.Collections.ObjectModel;


namespace Sem2Proj.Models;

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
        const string query = "SELECT * FROM AM_Assets ORDER BY Id ASC";
        using (var cmd = new SQLiteCommand(query, conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                AllAssets.Add(new AssetModel
                {
                    Id = Convert.ToInt32(reader["Id"]),
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

    public bool CreateNewAsset(string name, string imagePath, double maxHeat, double maxElectricity,
                          double productionCost, double emissions, double gasConsumption,
                          double oilConsumption, List<string> presetNames)
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

                // Add to all selected presets
                if (presetNames != null && presetNames.Count > 0)
                {
                    // Get all preset IDs at once for better performance
                    var presetIdMap = new Dictionary<string, int>();
                    const string getPresetIdsQuery = "SELECT Id, Name FROM AM_Presets WHERE Name IN ({0})";
                    var paramNames = presetNames.Select((_, i) => $"@name{i}").ToList();
                    var inClause = string.Join(",", paramNames);

                    using (var cmd = new SQLiteCommand(string.Format(getPresetIdsQuery, inClause), conn))
                    {
                        for (int i = 0; i < presetNames.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@name{i}", presetNames[i]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                presetIdMap[reader.GetString(1)] = reader.GetInt32(0);
                            }
                        }
                    }

                    // Insert into selected presets
                    foreach (var presetName in presetNames)
                    {
                        if (presetIdMap.TryGetValue(presetName, out int presetId))
                        {
                            const string addToPresetQuery = @"
                            INSERT INTO AM_PresetAssets (PresetId, AssetId)
                            VALUES (@presetId, @assetId)";

                            using (var cmd = new SQLiteCommand(addToPresetQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@presetId", presetId);
                                cmd.Parameters.AddWithValue("@assetId", newId);
                                cmd.ExecuteNonQuery();
                                Debug.WriteLine($"Added asset to preset {presetName} (ID: {presetId})");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Preset {presetName} not found");
                        }
                    }
                }

                // Refresh the local collections
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

    // Keep the old method for backward compatibility
    public bool CreateNewAsset(string name, string imagePath, double maxHeat, double maxElectricity,
                             double productionCost, double emissions, double gasConsumption,
                             double oilConsumption, string? presetName = null)
    {
        var presetNames = string.IsNullOrEmpty(presetName)
            ? new List<string>()
            : new List<string> { presetName };

        return CreateNewAsset(name, imagePath, maxHeat, maxElectricity, productionCost,
                            emissions, gasConsumption, oilConsumption, presetNames);
    }

    // In AssetManager.cs
    public void RefreshAssets()
    {
        // Clear existing collections
        AllAssets.Clear();
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

    public bool CreateNewPreset(string presetName)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Get the current maximum ID
                int newId = 1;
                const string getMaxIdQuery = "SELECT MAX(Id) FROM AM_Presets";
                using (var cmd = new SQLiteCommand(getMaxIdQuery, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        newId = Convert.ToInt32(result) + 1;
                    }
                }

                // Insert the new preset
                const string insertPresetQuery = "INSERT INTO AM_Presets (Id, Name) VALUES (@id, @name)";
                using (var cmd = new SQLiteCommand(insertPresetQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@name", presetName);
                    cmd.ExecuteNonQuery();
                }

                // Refresh the in-memory presets
                RefreshAssets();

                Debug.WriteLine($"New preset created with ID: {newId}, Name: {presetName}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating preset: {ex.Message}");
            return false;
        }
    }

 public bool UpdateAsset(
    int id,
    string name,
    string imageSource,
    double maxHeat,
    double maxElectricity,
    double productionCosts,
    double emissions,
    double gasConsumption,
    double oilConsumption)
{
    try
    {
        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();

            // First, get the current image source
            string currentImageSource = string.Empty;
            const string getImageQuery = "SELECT ImageSource FROM AM_Assets WHERE Id = @id";
            using (var cmd = new SQLiteCommand(getImageQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    currentImageSource = result.ToString();
                }
            }

            // Update all fields except ImageSource
            const string query = @"
            UPDATE AM_Assets SET
                Name = @name,
                MaxHeat = @maxHeat,
                MaxElectricity = @maxElectricity,
                ProductionCosts = @productionCosts,
                Emissions = @emissions,
                GasConsumption = @gasConsumption,
                OilConsumption = @oilConsumption
            WHERE Id = @id";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                cmd.Parameters.AddWithValue("@productionCosts", productionCosts);
                cmd.Parameters.AddWithValue("@emissions", emissions);
                cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    // Update the in-memory model
                    var asset = AllAssets.FirstOrDefault(a => a.Id == id);
                    if (asset != null)
                    {
                        asset.Name = name;
                        // Keep the original image source
                        asset.MaxHeat = maxHeat;
                        asset.MaxElectricity = maxElectricity;
                        asset.ProductionCosts = productionCosts;
                        asset.Emissions = emissions;
                        asset.GasConsumption = gasConsumption;
                        asset.OilConsumption = oilConsumption;
                    }
                    return true;
                }
                return false;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error updating asset ID {id}: {ex.Message}");
        return false;
    }
}

    public bool AddMachineToPreset(int presetId, int assetId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Verify preset exists
                const string checkPresetQuery = "SELECT COUNT(*) FROM AM_Presets WHERE Id = @presetId";
                using (var cmd = new SQLiteCommand(checkPresetQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        Debug.WriteLine($"Preset ID {presetId} not found");
                        return false;
                    }
                }

                // Verify asset exists
                const string checkAssetQuery = "SELECT COUNT(*) FROM AM_Assets WHERE Id = @assetId";
                using (var cmd = new SQLiteCommand(checkAssetQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        Debug.WriteLine($"Asset ID {assetId} not found");
                        return false;
                    }
                }

                // Check if association already exists
                const string checkExistsQuery =
                    "SELECT COUNT(*) FROM AM_PresetAssets WHERE PresetId = @presetId AND AssetId = @assetId";
                using (var cmd = new SQLiteCommand(checkExistsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                    {
                        Debug.WriteLine($"Asset {assetId} already in preset {presetId}");
                        return true;
                    }
                }

                // Add to the preset
                const string insertQuery =
                    "INSERT INTO AM_PresetAssets (PresetId, AssetId) VALUES (@presetId, @assetId)";
                using (var cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        var asset = AllAssets.FirstOrDefault(a => a.Id == assetId);

                        if (preset != null && asset != null && !preset.Machines.Contains(asset.Name))
                        {
                            preset.Machines.Add(asset.Name);
                        }

                        Debug.WriteLine($"Successfully added asset {assetId} to preset {presetId}");
                        return true;
                    }
                }

                Debug.WriteLine($"Failed to add asset {assetId} to preset {presetId}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding asset {assetId} to preset {presetId}: {ex.Message}");
            return false;
        }
    }
    public bool IsMachineInPreset(int presetId, int assetId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = @"
                SELECT COUNT(*) 
                FROM AM_PresetAssets 
                WHERE PresetId = @presetId AND AssetId = @assetId";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking preset membership: {ex.Message}");
            return false;
        }
    }

    public bool RemoveMachineFromPreset(int presetId, int assetId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = @"
                DELETE FROM AM_PresetAssets 
                WHERE PresetId = @presetId AND AssetId = @assetId";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        var asset = AllAssets.FirstOrDefault(a => a.Id == assetId);
                        if (preset != null && asset != null)
                        {
                            preset.Machines.Remove(asset.Name);
                        }
                        return true;
                    }
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing machine from preset: {ex.Message}");
            return false;
        }
    }

    public bool DeleteMachine(int machineId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // First delete from preset associations
                const string deletePresetAssociations =
                    "DELETE FROM AM_PresetAssets WHERE AssetId = @machineId";

                using (var cmd = new SQLiteCommand(deletePresetAssociations, conn))
                {
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    cmd.ExecuteNonQuery();
                }

                // Then delete the machine itself
                const string deleteMachine =
                    "DELETE FROM AM_Assets WHERE Id = @machineId";

                using (var cmd = new SQLiteCommand(deleteMachine, conn))
                {
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update in-memory collections
                        AllAssets.RemoveAll(a => a.Id == machineId);
                        foreach (var preset in Presets)
                        {
                            var machine = AllAssets.FirstOrDefault(a => a.Id == machineId);
                            if (machine != null)
                            {
                                preset.Machines.Remove(machine.Name);
                            }
                        }

                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting machine: {ex.Message}");
            return false;
        }
    }
}



public class Preset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Machines { get; set; } = new();
    public ICommand? NavigateToPresetCommand { get; set; }
    public string PresetName => Name;  // Read-only property that returns Name
    public bool IsSelected { get; set; } = false;

    // Constructor to properly initialize
    public Preset()
    {
    }

    // Helper method to update selection state
    public void UpdateSelectionForMachine(string machineName)
    {
        IsSelected = Machines.Contains(machineName);
    }



}

public partial class AssetModel : ObservableObject
{
    [ObservableProperty]
    private int _id;  // Add this
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string imageSource = string.Empty;
    [ObservableProperty] private Bitmap? _imageFromBinding;
    [ObservableProperty] private double maxHeat;
    [ObservableProperty] private double productionCosts;
    [ObservableProperty] private double emissions;
    [ObservableProperty] private double gasConsumption;
    [ObservableProperty] private double oilConsumption;
    [ObservableProperty] private double maxElectricity;
    [ObservableProperty] private ICommand? removeFromPresetCommand;
    [ObservableProperty] private ICommand? deleteCommand;

    public ObservableCollection<Preset> AvailablePresets { get; set; } = new();

    public bool IsElectricBoiler => MaxElectricity < 0;
    public bool IsGenerator => MaxElectricity > 0;
    public double CostPerMW => ProductionCosts;
    public double EmissionsPerMW => MaxHeat > 0 ? Emissions / MaxHeat : 0;

    [ObservableProperty]
    private ObservableCollection<PresetSelectionItem> _presetSelections = new();

    public void InitializePresetSelections(IEnumerable<Preset> allPresets)
    {
        Debug.WriteLine($"[InitializePresetSelections] Start for machine: {Name}");
        AvailablePresets.Clear();

        foreach (var presetTemplate in allPresets)
        {
            bool isSelected = presetTemplate.Machines.Contains(Name);

            var preset = new Preset
            {
                Id = presetTemplate.Id,
                Name = presetTemplate.Name,
                Machines = new List<string>(presetTemplate.Machines),
                NavigateToPresetCommand = presetTemplate.NavigateToPresetCommand,
                IsSelected = isSelected // Directly set from the Machines list
            };

            // Double verification
            Debug.WriteLine($"- Preset '{preset.Name}' (ID: {preset.Id})");
            Debug.WriteLine($"  Machines in preset: {string.Join(", ", preset.Machines)}");
            Debug.WriteLine($"  Current machine '{Name}' in preset: {isSelected}");
            Debug.WriteLine($"  IsSelected set to: {preset.IsSelected}");

            // Verify the UpdateSelectionForMachine matches our direct setting
            preset.UpdateSelectionForMachine(Name);
            if (preset.IsSelected != isSelected)
            {
                Debug.WriteLine($"  WARNING: UpdateSelectionForMachine gave different result! {preset.IsSelected}");
            }

            AvailablePresets.Add(preset);
        }

        Debug.WriteLine($"[InitializePresetSelections] Completed for machine: {Name}");
        Debug.WriteLine($"Final preset states:");
        foreach (var p in AvailablePresets)
        {
            Debug.WriteLine($"- {p.Name}: {p.IsSelected}");
        }
    }

}

public class PresetSelectionItem : ObservableObject
{
    public string PresetName { get; }
    public bool IsSelected { get; set; }

    public PresetSelectionItem(string presetName, bool isSelected)
    {
        PresetName = presetName;
        IsSelected = isSelected;
    }
}
public partial class HeatingGrid : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _imageSource = string.Empty;
    [ObservableProperty] private string _architecture = string.Empty;
    [ObservableProperty] private string _size = string.Empty;
}