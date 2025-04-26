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
        // Load preset definitions
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

        // Load machines for each preset from the PresetMachines table
        foreach (var preset in Presets)
        {
            const string machineQuery = @"
            SELECT * 
            FROM PresetMachines
            WHERE PresetId = @presetId";

            using (var machineCmd = new SQLiteCommand(machineQuery, conn))
            {
                machineCmd.Parameters.AddWithValue("@presetId", preset.Id);
                using (var machineReader = machineCmd.ExecuteReader())
                {
                    while (machineReader.Read())
                    {
                        var machine = new AssetModel
                        {
                            Id = Convert.ToInt32(machineReader["PresetId"]),
                            Name = machineReader["Name"].ToString() ?? string.Empty,
                            ImageSource = machineReader["ImageSource"].ToString() ?? string.Empty,
                            MaxHeat = Convert.ToDouble(machineReader["MaxHeat"]),
                            ProductionCosts = Convert.ToDouble(machineReader["ProductionCosts"]),
                            Emissions = Convert.ToDouble(machineReader["Emissions"]),
                            GasConsumption = Convert.ToDouble(machineReader["GasConsumption"]),
                            OilConsumption = Convert.ToDouble(machineReader["OilConsumption"]),
                            MaxElectricity = Convert.ToDouble(machineReader["MaxElectricity"])
                        };
                        preset.MachineModels.Add(machine);
                    }
                }
            }
        }

        Debug.WriteLine($"Loaded {Presets.Count} presets from database.");
        foreach (var preset in Presets)
        {
            Debug.WriteLine($"Preset: {preset.Name}, Machines: {preset.MachineModels.Count}");
        }
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

        // Convert ObservableCollection to List
        CurrentAssets = preset.MachineModels.ToList();

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
                    cmd.Parameters.AddWithValue("@imageSource", imagePath);
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

    public bool RemoveMachineFromPreset(int presetId, int machineId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = @"
                DELETE FROM PresetMachines 
                WHERE PresetId = @presetId AND Id = @machineId";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        var machine = preset?.MachineModels.FirstOrDefault(m => m.Id == machineId);
                        if (preset != null && machine != null)
                        {
                            preset.MachineModels.Remove(machine);
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

    public bool AddMachineToPreset(int presetId, AssetModel machine)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = @"
                INSERT INTO PresetMachines (PresetId, Name, ImageSource, MaxHeat, ProductionCosts, Emissions, GasConsumption, OilConsumption, MaxElectricity)
                VALUES (@presetId, @name, @imageSource, @maxHeat, @productionCosts, @emissions, @gasConsumption, @oilConsumption, @maxElectricity)";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@name", machine.Name);
                    cmd.Parameters.AddWithValue("@imageSource", machine.ImageSource);
                    cmd.Parameters.AddWithValue("@maxHeat", machine.MaxHeat);
                    cmd.Parameters.AddWithValue("@productionCosts", machine.ProductionCosts);
                    cmd.Parameters.AddWithValue("@emissions", machine.Emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", machine.GasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", machine.OilConsumption);
                    cmd.Parameters.AddWithValue("@maxElectricity", machine.MaxElectricity);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        if (preset != null)
                        {
                            preset.MachineModels.Add(machine);
                        }
                        return true;
                    }
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding machine to preset: {ex.Message}");
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


    public bool DeletePreset(int presetId)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Delete preset associations first
                const string deleteAssociationsQuery = "DELETE FROM AM_PresetAssets WHERE PresetId = @presetId";
                using (var cmd = new SQLiteCommand(deleteAssociationsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.ExecuteNonQuery();
                }

                // Delete the preset itself
                const string deletePresetQuery = "DELETE FROM AM_Presets WHERE Id = @presetId";
                using (var cmd = new SQLiteCommand(deletePresetQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Update in-memory collection
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        if (preset != null)
                        {
                            Presets.Remove(preset);
                        }
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting preset: {ex.Message}");
        }
        return false;
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
    public bool UpdatePresetName(int presetId, string newName)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = "UPDATE AM_Presets SET Name = @newName WHERE Id = @presetId";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        if (preset != null)
                        {
                            preset.Name = newName;
                        }
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating preset name: {ex.Message}");
        }
        return false;
    }

public bool UpdateMachineInPreset(
    int presetId,
    string name,
    double maxHeat,
    double maxElectricity,
    double productionCosts,
    double emissions,
    double gasConsumption,
    double oilConsumption,
    bool isActive,
    double heatProduction)
{
    try
    {
        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();

            const string updateQuery = @"
                UPDATE PresetMachines
                SET MaxHeat = @maxHeat,
                    MaxElectricity = @maxElectricity,
                    ProductionCosts = @productionCosts,
                    Emissions = @emissions,
                    GasConsumption = @gasConsumption,
                    OilConsumption = @oilConsumption,
                    IsActive = @isActive,
                    HeatProduction = @heatProduction
                WHERE PresetId = @presetId AND Name = @name";

            using (var updateCmd = new SQLiteCommand(updateQuery, conn))
            {
                updateCmd.Parameters.AddWithValue("@presetId", presetId);
                updateCmd.Parameters.AddWithValue("@name", name);
                updateCmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                updateCmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                updateCmd.Parameters.AddWithValue("@productionCosts", productionCosts);
                updateCmd.Parameters.AddWithValue("@emissions", emissions);
                updateCmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                updateCmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                updateCmd.Parameters.AddWithValue("@isActive", isActive);
                updateCmd.Parameters.AddWithValue("@heatProduction", heatProduction);

                int rowsAffected = updateCmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error updating machine in preset: {ex.Message}");
        return false;
    }
}
}



public class Preset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Machines { get; set; } = new();
    public ObservableCollection<AssetModel> MachineModels { get; set; } = new();
    public ICommand? NavigateToPresetCommand { get; set; }
    public ICommand? DeletePresetCommand { get; set; }
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
    [ObservableProperty] private int _id;
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

    // New properties
    [ObservableProperty]
    private bool isActive; // Represents whether the machine is active in a preset

    [ObservableProperty]
    private double heatProduction; // Represents the current heat production of the machine

    public void InitializePresetSelections(IEnumerable<Preset> allPresets)
    {
        AvailablePresets.Clear();

        foreach (var preset in allPresets)
        {
            bool isSelected = preset.MachineModels.Any(m => m.Id == Id);

            AvailablePresets.Add(new Preset
            {
                Id = preset.Id,
                Name = preset.Name,
                IsSelected = isSelected
            });
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