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
using CommunityToolkit.Mvvm.Input;


namespace Sem2Proj.Models;

public class AssetManager
{
    private readonly string _dbPath = "Data Source=Data/heat_optimization.db;Version=3;";

    public HeatingGrid? GridInfo { get; private set; }

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

        // Load machines for each preset
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
                            OriginalName = machineReader["Name"].ToString() ?? string.Empty, // Set OriginalName
                            ImageSource = machineReader["ImageSource"].ToString() ?? string.Empty,
                            MaxHeat = Convert.ToDouble(machineReader["MaxHeat"]),
                            ProductionCosts = Convert.ToDouble(machineReader["ProductionCosts"]),
                            Emissions = Convert.ToDouble(machineReader["Emissions"]),
                            GasConsumption = Convert.ToDouble(machineReader["GasConsumption"]),
                            OilConsumption = Convert.ToDouble(machineReader["OilConsumption"]),
                            MaxElectricity = Convert.ToDouble(machineReader["MaxElectricity"]),
                            IsActive = Convert.ToBoolean(machineReader["IsActive"]),
                            HeatProduction = Convert.ToDouble(machineReader["HeatProduction"]),
                            Color = machineReader["Color"].ToString() ?? "#FFFFFF"
                        };

                        preset.MachineModels.Add(machine);
                    }
                }
            }
        }

        Debug.WriteLine($"Loaded {Presets.Count} presets from database.");
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

    public bool CreateNewMachine(string name, string imagePath, double maxHeat, double maxElectricity,
                              double productionCost, double emissions, double gasConsumption,
                              double oilConsumption, int presetId, string color) // Added color parameter
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Insert the new machine into the PresetMachines table
                const string insertQuery = @"
            INSERT INTO PresetMachines 
            (PresetId, Name, ImageSource, MaxHeat, MaxElectricity, ProductionCosts, Emissions, GasConsumption, OilConsumption, IsActive, HeatProduction, Color)
            VALUES
            (@presetId, @name, @imageSource, @maxHeat, @maxElectricity, @productionCosts, @emissions, @gasConsumption, @oilConsumption, @isActive, @heatProduction, @color)";

                using (var cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@imageSource", imagePath);
                    cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                    cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                    cmd.Parameters.AddWithValue("@productionCosts", productionCost);
                    cmd.Parameters.AddWithValue("@emissions", emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                    cmd.Parameters.AddWithValue("@isActive", true); // Default to active
                    cmd.Parameters.AddWithValue("@heatProduction", maxHeat); // Default heat production
                    cmd.Parameters.AddWithValue("@color", color); // Bind color parameter

                    cmd.ExecuteNonQuery();
                }

                Debug.WriteLine($"New machine '{name}' created in PresetId {presetId}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating machine: {ex.Message}");
            return false;
        }
    }

    public void RefreshAssets()
    {
        // Clear existing collections

        Presets.Clear();

        // Reload from database
        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();
            LoadAllPresets(conn);
        }

        // Reapply current scenario if one was selected
        if (SelectedScenarioIndex >= 0)
        {
            SetScenario(SelectedScenarioIndex);
        }
    }

    public bool RemoveMachineFromPreset(int presetId, string machineName)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();
                const string query = @"
                DELETE FROM PresetMachines 
                WHERE PresetId = @presetId AND Name = @machineName";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@machineName", machineName);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Update in-memory model
                        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
                        var machine = preset?.MachineModels.FirstOrDefault(m => m.Name == machineName);
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


  public bool AddMachineToPreset(int presetId, AssetModel machine)
{
    try
    {
        using (var conn = new SQLiteConnection(_dbPath))
        {
            conn.Open();

            // Check if the machine already exists in the preset
            const string checkQuery = @"
            SELECT COUNT(*) 
            FROM PresetMachines 
            WHERE PresetId = @presetId AND Name = @name";

            using (var checkCmd = new SQLiteCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@presetId", presetId);
                checkCmd.Parameters.AddWithValue("@name", machine.Name);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    Debug.WriteLine($"Machine '{machine.Name}' already exists in PresetId {presetId}. Skipping insertion.");
                    return true;
                }
            }

            // Insert the machine into the preset
            const string insertQuery = @"
            INSERT INTO PresetMachines (PresetId, Name, MaxHeat, ProductionCosts, Emissions, GasConsumption, OilConsumption, MaxElectricity, IsActive, HeatProduction, Color)
            VALUES (@presetId, @name, @maxHeat, @productionCosts, @emissions, @gasConsumption, @oilConsumption, @maxElectricity, @isActive, @heatProduction, @color)";

            using (var insertCmd = new SQLiteCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@presetId", presetId);
                insertCmd.Parameters.AddWithValue("@name", machine.Name);
                insertCmd.Parameters.AddWithValue("@maxHeat", machine.MaxHeat);
                insertCmd.Parameters.AddWithValue("@productionCosts", machine.ProductionCosts);
                insertCmd.Parameters.AddWithValue("@emissions", machine.Emissions);
                insertCmd.Parameters.AddWithValue("@gasConsumption", machine.GasConsumption);
                insertCmd.Parameters.AddWithValue("@oilConsumption", machine.OilConsumption);
                insertCmd.Parameters.AddWithValue("@maxElectricity", machine.MaxElectricity);
                insertCmd.Parameters.AddWithValue("@isActive", machine.IsActive);
                insertCmd.Parameters.AddWithValue("@heatProduction", machine.HeatProduction);
                insertCmd.Parameters.AddWithValue("@color", machine.Color); // Bind color parameter

                insertCmd.ExecuteNonQuery();
            }

            return true;
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

    public bool DeleteMachineFromPreset(int presetId, string machineName)
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Delete the machine from the PresetMachines table
                const string deleteQuery = @"
                DELETE FROM PresetMachines 
                WHERE PresetId = @presetId AND Name = @machineName";

                using (var cmd = new SQLiteCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@machineName", machineName);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Debug.WriteLine($"Machine '{machineName}' deleted from PresetId {presetId}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Machine '{machineName}' not found in PresetId {presetId}");
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting machine: {ex.Message}");
            return false;
        }
    }

    public bool UpdateMachineInPreset(
        int presetId,
        string originalName,
        string newName,
        double maxHeat,
        double maxElectricity,
        double productionCosts,
        double emissions,
        double gasConsumption,
        double oilConsumption,
        bool isActive,
        double heatProduction,
        string color) // Added color parameter
    {
        try
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.Open();

                // Update the machine in the PresetMachines table
                const string updateQuery = @"
            UPDATE PresetMachines
            SET Name = @newName,
                MaxHeat = @maxHeat,
                MaxElectricity = @maxElectricity,
                ProductionCosts = @productionCosts,
                Emissions = @emissions,
                GasConsumption = @gasConsumption,
                OilConsumption = @oilConsumption,
                IsActive = @isActive,
                HeatProduction = @heatProduction,
                Color = @color
            WHERE PresetId = @presetId AND Name = @originalName";

                using (var cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@originalName", originalName);
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                    cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                    cmd.Parameters.AddWithValue("@productionCosts", productionCosts);
                    cmd.Parameters.AddWithValue("@emissions", emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                    cmd.Parameters.AddWithValue("@isActive", isActive);
                    cmd.Parameters.AddWithValue("@heatProduction", heatProduction);
                    cmd.Parameters.AddWithValue("@color", color); // Bind color parameter

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Debug.WriteLine($"Machine '{newName}' updated in PresetId {presetId}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Machine '{originalName}' not found in PresetId {presetId}");
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating machine: {ex.Message}");
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
}



public partial class Preset : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Machines { get; set; } = new();
    public ObservableCollection<AssetModel> MachineModels { get; set; } = new();
    public ICommand? NavigateToPresetCommand { get; set; }

    public ICommand? DeletePresetCommand { get; set; }



    public string PresetName => Name;

    public bool IsSelected { get; set; } = false;

    [ObservableProperty] private bool _isPresetSelected;
    private Action<Preset>? _selectPresetAction;

    public void SetSelectPresetAction(Action<Preset> action)
    {
        _selectPresetAction = action;
    }

    // Command that will be called when radio button is clicked
    public ICommand SelectPresetCommand => new RelayCommand(() =>
    {
        _selectPresetAction?.Invoke(this);
    });
    [ObservableProperty] private bool _isRenaming;

    public ICommand StartRenamingCommand => new RelayCommand(StartRenaming);

    private void StartRenaming()
    {
        IsRenaming = true;
    }

    public ICommand FinishRenamingCommand => new RelayCommand(FinishRenaming);

    private void FinishRenaming()
    {
        IsRenaming = false;
    }

    public Preset()
    {
    }

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
    [ObservableProperty] private ICommand? deleteMachineCommand; // Command to delete the machine (Configure View)

    [ObservableProperty] private string originalName = string.Empty;

    [ObservableProperty] private string color;
    public ObservableCollection<Preset> AvailablePresets { get; set; } = new();

    public bool IsElectricBoiler => MaxElectricity < 0;
    public bool IsGenerator => MaxElectricity > 0;
    public double CostPerMW => ProductionCosts;
    public double EmissionsPerMW => MaxHeat > 0 ? Emissions / MaxHeat : 0;

    [ObservableProperty]
    private ObservableCollection<PresetSelectionItem> _presetSelections = new();

    [ObservableProperty]
    private bool isActive; // Represents whether the machine is active in a preset

    [ObservableProperty]
    private double heatProduction; // Represents the current heat production of the machine

    [ObservableProperty]
    private ObservableCollection<AssetModel> machineModels = new(); // Represents the collection of machines in the preset

    public void InitializePresetSelections(IEnumerable<Preset> allPresets)
    {
        PresetSelections.Clear();

        foreach (var preset in allPresets)
        {
            Debug.WriteLine($"[InitializePresetSelections] Checking Machine: {Name} against Preset: {preset.Name}");
            Debug.WriteLine($"[InitializePresetSelections] Machines in Preset: {string.Join(", ", preset.Machines)}");

            bool isSelected = preset.Machines.Any(machineName =>
                string.Equals(machineName.Trim(), Name.Trim(), StringComparison.OrdinalIgnoreCase)); // Case-insensitive comparison

            Debug.WriteLine($"[InitializePresetSelections] Machine: {Name}, Preset: {preset.Name}, IsSelected: {isSelected}");
            PresetSelections.Add(new PresetSelectionItem(preset.Name, isSelected));
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