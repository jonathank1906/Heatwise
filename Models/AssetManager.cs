using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;


namespace Sem2Proj.Models;

public class AssetManager
{
    private readonly string dbPath = "Data Source=Data/heat_optimization.db;";

    public HeatingGrid? GridInfo { get; private set; }

    public List<AssetModel> CurrentAssets { get; private set; } = new();
    public ObservableCollection<Preset> Presets { get; private set; } = new ObservableCollection<Preset>();

    public string CurrentScenarioName => Presets.Count > 0 && SelectedScenarioIndex >= 0
        ? Presets[SelectedScenarioIndex].Name
        : "No Scenario";

    public int SelectedScenarioIndex { get; private set; } = -1;

    public ICommand RestoreDefaultsCommand { get; private set; }

    public AssetManager()
    {
        LoadAssetsAndPresetsFromDatabase();

        if (Presets.Count > 0)
        {
            SetScenario(0);
        }

        RestoreDefaultsCommand = new RelayCommand(() =>
        {
            RestoreDefaults();
        });
    }

    private void LoadAssetsAndPresetsFromDatabase()
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
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

    private void LoadAllPresets(SqliteConnection conn)
    {
        // Load preset definitions
        const string presetQuery = "SELECT * FROM AM_Presets";
        using (var presetCmd = new SqliteCommand(presetQuery, conn))
        using (var presetReader = presetCmd.ExecuteReader())
        {
            bool isFirstPreset = true;
            while (presetReader.Read())
            {
                var preset = new Preset
                {
                    Id = Convert.ToInt32(presetReader["Id"]),
                    Name = presetReader["Name"].ToString() ?? string.Empty,
                    IsPresetSelected = isFirstPreset // Set first preset as selected
                };
                Presets.Add(preset);
                isFirstPreset = false;
            }
        }
        // Load machines for each preset
        foreach (var preset in Presets)
        {
            const string machineQuery = @"
        SELECT * 
        FROM PresetMachines
        WHERE PresetId = @presetId";

            using (var machineCmd = new SqliteCommand(machineQuery, conn))
            {
                machineCmd.Parameters.AddWithValue("@presetId", preset.Id);
                using (var machineReader = machineCmd.ExecuteReader())
                {
                    while (machineReader.Read())
                    {
                        try
                        {
                            var machine = new AssetModel
                            {
                                Id = machineReader["Id"] != DBNull.Value ? Convert.ToInt32(machineReader["Id"]) : 0,
                                PresetId = machineReader["PresetId"] != DBNull.Value ? Convert.ToInt32(machineReader["PresetId"]) : 0,
                                Name = machineReader["Name"].ToString() ?? string.Empty,
                                OriginalName = machineReader["Name"].ToString() ?? string.Empty,
                                ImageSource = machineReader["ImageSource"].ToString() ?? string.Empty,
                                MaxHeat = machineReader["MaxHeat"] != DBNull.Value ? Convert.ToDouble(machineReader["MaxHeat"]) : 0,
                                ProductionCosts = machineReader["ProductionCosts"] != DBNull.Value ? Convert.ToDouble(machineReader["ProductionCosts"]) : 0,
                                Emissions = machineReader["Emissions"] != DBNull.Value ? Convert.ToDouble(machineReader["Emissions"]) : 0,
                                GasConsumption = machineReader["GasConsumption"] != DBNull.Value ? Convert.ToDouble(machineReader["GasConsumption"]) : 0,
                                OilConsumption = machineReader["OilConsumption"] != DBNull.Value ? Convert.ToDouble(machineReader["OilConsumption"]) : 0,
                                MaxElectricity = machineReader["MaxElectricity"] != DBNull.Value ? Convert.ToDouble(machineReader["MaxElectricity"]) : 0,
                                IsActive = machineReader["IsActive"] != DBNull.Value && Convert.ToBoolean(machineReader["IsActive"]),
                                HeatProduction = machineReader["HeatProduction"] != DBNull.Value ? Convert.ToDouble(machineReader["HeatProduction"]) : 0,
                                Color = machineReader["Color"].ToString() ?? "#FFFFFF"
                            };

                            Debug.WriteLine($"Loaded Image: {machine.ImageSource}");
                            preset.MachineModels.Add(machine);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing machine data: {ex.Message}");
                        }
                    }
                }
            }
        }

        Debug.WriteLine($"Loaded {Presets.Count} presets from database.");
    }

    private void LoadHeatingGridInfo(SqliteConnection conn)
    {
        const string query = "SELECT * FROM AM_HeatingGrid LIMIT 1";
        using (var cmd = new SqliteCommand(query, conn))
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
                Debug.WriteLine($"Loaded heating grid image: {GridInfo.ImageSource}");
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
                            double oilConsumption, int presetId, string color)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // Get the next available ID
                int newId = 1;
                const string getMaxIdQuery = "SELECT MAX(Id) FROM PresetMachines";
                using (var cmd = new SqliteCommand(getMaxIdQuery, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        newId = Convert.ToInt32(result) + 1;
                    }
                }

                // Insert the new machine with the calculated ID
                const string insertQuery = @"
                INSERT INTO PresetMachines 
                (Id, PresetId, Name, ImageSource, MaxHeat, MaxElectricity, 
                 ProductionCosts, Emissions, GasConsumption, OilConsumption, 
                 IsActive, HeatProduction, Color)
                VALUES
                (@id, @presetId, @name, @imageSource, @maxHeat, @maxElectricity, 
                 @productionCosts, @emissions, @gasConsumption, @oilConsumption, 
                 @isActive, @heatProduction, @color)";

                using (var cmd = new SqliteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@imageSource", imagePath);
                    cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                    cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                    cmd.Parameters.AddWithValue("@productionCosts", productionCost);
                    cmd.Parameters.AddWithValue("@emissions", emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                    cmd.Parameters.AddWithValue("@isActive", true);
                    cmd.Parameters.AddWithValue("@heatProduction", maxHeat);
                    cmd.Parameters.AddWithValue("@color", color);

                    cmd.ExecuteNonQuery();
                }

                Debug.WriteLine($"New machine '{name}' created with ID {newId} in PresetId {presetId}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating machine: {ex.Message}");
            return false;
        }
    }
    public List<AssetModel> GetActiveMachinesForCurrentPreset()
    {
        var currentPreset = Presets.FirstOrDefault(p => p.IsPresetSelected);
        if (currentPreset == null) return new List<AssetModel>();

        return currentPreset.MachineModels
            .Where(m => m.IsActive)
            .ToList();
    }

    public void RefreshAssets()
    {
        // Clear existing collections

        Presets.Clear();

        // Reload from database
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            LoadAllPresets(conn);
        }

        SetScenario(0);

    }

    public bool RemoveMachineFromPreset(int machineId) // Removed presetId parameter since we only need machineId
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // First verify the machine exists
                const string verifyQuery = "SELECT COUNT(*) FROM PresetMachines WHERE Id = @machineId";
                using (var verifyCmd = new SqliteCommand(verifyQuery, conn))
                {
                    verifyCmd.Parameters.AddWithValue("@machineId", machineId);
                    var count = (long)verifyCmd.ExecuteScalar();
                    Debug.WriteLine($"Found {count} machines matching machineId:{machineId}");
                }

                const string query = "DELETE FROM PresetMachines WHERE Id = @machineId";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Rows affected by delete: {rowsAffected}");

                    if (rowsAffected > 0)
                    {
                        // Update in-memory model - find machine by ID only
                        foreach (var preset in Presets)
                        {
                            var machine = preset.MachineModels.FirstOrDefault(m => m.Id == machineId);
                            if (machine != null)
                            {
                                preset.MachineModels.Remove(machine);
                                break;
                            }
                        }
                        return true;
                    }
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing machine: {ex.Message}");
            return false;
        }
    }

    public bool CreateNewPreset(string presetName)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // Get the current maximum ID
                int newId = 1;
                const string getMaxIdQuery = "SELECT MAX(Id) FROM AM_Presets";
                using (var cmd = new SqliteCommand(getMaxIdQuery, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        newId = Convert.ToInt32(result) + 1;
                    }
                }

                // Insert the new preset
                const string insertPresetQuery = "INSERT INTO AM_Presets (Id, Name) VALUES (@id, @name)";
                using (var cmd = new SqliteCommand(insertPresetQuery, conn))
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // Check if the machine already exists in the preset
                const string checkQuery = @"
            SELECT COUNT(*) 
            FROM PresetMachines 
            WHERE PresetId = @presetId AND Name = @name";

                using (var checkCmd = new SqliteCommand(checkQuery, conn))
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

                using (var insertCmd = new SqliteCommand(insertQuery, conn))
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                const string query = @"
                SELECT COUNT(*) 
                FROM AM_PresetAssets 
                WHERE PresetId = @presetId AND AssetId = @assetId";

                using (var cmd = new SqliteCommand(query, conn))
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // Delete preset associations first
                const string deleteAssociationsQuery = "DELETE FROM AM_PresetAssets WHERE PresetId = @presetId";
                using (var cmd = new SqliteCommand(deleteAssociationsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@presetId", presetId);
                    cmd.ExecuteNonQuery();
                }

                // Delete the preset itself
                const string deletePresetQuery = "DELETE FROM AM_Presets WHERE Id = @presetId";
                using (var cmd = new SqliteCommand(deletePresetQuery, conn))
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();

                // Delete the machine from the PresetMachines table
                const string deleteQuery = @"
                DELETE FROM PresetMachines 
                WHERE PresetId = @presetId AND Name = @machineName";

                using (var cmd = new SqliteCommand(deleteQuery, conn))
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
         int machineId,
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
            using (var conn = new SqliteConnection(dbPath))
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
            WHERE Id = @machineId";

                using (var cmd = new SqliteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@machineId", machineId);
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                    cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                    cmd.Parameters.AddWithValue("@productionCosts", productionCosts);
                    cmd.Parameters.AddWithValue("@emissions", emissions);
                    cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                    cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                    cmd.Parameters.AddWithValue("@isActive", isActive);
                    cmd.Parameters.AddWithValue("@heatProduction", heatProduction);
                    cmd.Parameters.AddWithValue("@color", color);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Debug.WriteLine($"Machine ID {machineId} updated to name '{newName}'");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Machine ID {machineId} not found");
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                const string query = "UPDATE AM_Presets SET Name = @newName WHERE Id = @presetId";
                using (var cmd = new SqliteCommand(query, conn))
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

    public void RestoreDefaults()
    {
        Debug.WriteLine("Starting RestoreDefaults...");

        using (var conn = new SqliteConnection(dbPath))
        {
            Debug.WriteLine($"Using database file: {dbPath}");
            conn.Open();

            // Temporarily disable foreign key enforcement
            Debug.WriteLine("Disabling foreign key enforcement...");
            using (var disableForeignKeysCmd = new SqliteCommand("PRAGMA foreign_keys = OFF;", conn))
            {
                disableForeignKeysCmd.ExecuteNonQuery();
                Debug.WriteLine("Foreign key enforcement disabled.");
            }

            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Step 1: Clear the PresetMachines table to avoid foreign key violations
                    Debug.WriteLine("Clearing PresetMachines table...");
                    const string clearPresetMachinesQuery = "DELETE FROM PresetMachines";
                    using (var cmd = new SqliteCommand(clearPresetMachinesQuery, conn))
                    {
                        cmd.Transaction = transaction;
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Debug.WriteLine($"PresetMachines table cleared. Rows affected: {rowsAffected}");
                    }

                    // Step 2: Delete all presets
                    Debug.WriteLine("Clearing AM_Presets table...");
                    const string deletePresetsQuery = "DELETE FROM AM_Presets";
                    using (var cmd = new SqliteCommand(deletePresetsQuery, conn))
                    {
                        cmd.Transaction = transaction;
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Debug.WriteLine($"AM_Presets table cleared. Rows affected: {rowsAffected}");
                    }

                    // Step 3: Recreate Scenario 1
                    Debug.WriteLine("Inserting Scenario 1...");
                    const string insertScenario1Query = "INSERT INTO AM_Presets (Id, Name) VALUES (1, 'Scenario 1')";
                    using (var cmd = new SqliteCommand(insertScenario1Query, conn))
                    {
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Scenario 1 inserted.");
                    }

                    // Step 4: Recreate Scenario 2
                    Debug.WriteLine("Inserting Scenario 2...");
                    const string insertScenario2Query = "INSERT INTO AM_Presets (Id, Name) VALUES (2, 'Scenario 2')";
                    using (var cmd = new SqliteCommand(insertScenario2Query, conn))
                    {
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Scenario 2 inserted.");
                    }

                    // Step 5: Read CSV and restore default machine settings
                    Debug.WriteLine("Reading CSV file for PresetMachines...");
                    string csvPath = "Data/PresetMachines_backup.csv";
                    if (!File.Exists(csvPath))
                    {
                        Debug.WriteLine($"CSV file not found: {csvPath}");
                        throw new FileNotFoundException($"CSV file not found: {csvPath}");
                    }

                    string[] lines = File.ReadAllLines(csvPath);
                    Debug.WriteLine($"CSV file loaded. Total lines: {lines.Length}");

                    // Skip header row if present
                    int startIndex = lines.Length > 0 && lines[0].StartsWith("Id,") ? 1 : 0;

                    // Process each line
                    for (int i = startIndex; i < lines.Length; i++)
                    {
                        string[] values = lines[i].Split(',');

                        // Validate the CSV line
                        if (values.Length < 12)
                        {
                            Debug.WriteLine($"Skipping malformed CSV line: {lines[i]}");
                            continue;
                        }

                        // Parse values from the CSV
                        if (!int.TryParse(values[0], out int machineId))
                        {
                            Debug.WriteLine($"Skipping CSV line with invalid Machine Id: {lines[i]}");
                            continue;
                        }

                        if (!int.TryParse(values[1], out int presetId))
                        {
                            Debug.WriteLine($"Skipping CSV line with invalid Preset Id for machine {machineId}: {lines[i]}");
                            continue;
                        }

                        string name = values[2];
                        string imageSource = values[3];
                        if (!double.TryParse(values[4], out double maxHeat)) { Debug.WriteLine($"Invalid MaxHeat for machine {machineId}"); continue; }
                        if (!double.TryParse(values[5], out double maxElectricity)) { Debug.WriteLine($"Invalid MaxElectricity for machine {machineId}"); continue; }
                        if (!double.TryParse(values[6], out double productionCosts)) { Debug.WriteLine($"Invalid ProductionCosts for machine {machineId}"); continue; }
                        if (!double.TryParse(values[7], out double emissions)) { Debug.WriteLine($"Invalid Emissions for machine {machineId}"); continue; }
                        if (!double.TryParse(values[8], out double gasConsumption)) { Debug.WriteLine($"Invalid GasConsumption for machine {machineId}"); continue; }
                        if (!double.TryParse(values[9], out double oilConsumption)) { Debug.WriteLine($"Invalid OilConsumption for machine {machineId}"); continue; }

                        bool isActive = values[10] == "1" || (values[10].Length > 0 && values[10].ToLower() == "true");
                        if (!double.TryParse(values[11], out double heatProduction)) { Debug.WriteLine($"Invalid HeatProduction for machine {machineId}"); continue; }
                        string color = values[12];

                        // Insert or update the machine in the database
                        Debug.WriteLine($"Inserting or updating machine with Id: {machineId}, PresetId: {presetId}");
                        const string insertMachineQuery = @"
                        INSERT INTO PresetMachines (Id, PresetId, Name, ImageSource, MaxHeat,  
                                                    ProductionCosts, Emissions, GasConsumption, OilConsumption, MaxElectricity,
                                                    IsActive, HeatProduction, Color)
                        VALUES (@machineId, @presetId, @name, @imageSource, @maxHeat, @maxElectricity, 
                                @productionCosts, @emissions, @gasConsumption, @oilConsumption, 
                                @isActive, @heatProduction, @color)
                        ON CONFLICT(Id) DO UPDATE SET
                            PresetId = excluded.PresetId,
                            Name = excluded.Name,
                            ImageSource = excluded.ImageSource,
                            MaxHeat = excluded.MaxHeat,
                            MaxElectricity = excluded.MaxElectricity,
                            ProductionCosts = excluded.ProductionCosts,
                            Emissions = excluded.Emissions,
                            GasConsumption = excluded.GasConsumption,
                            OilConsumption = excluded.OilConsumption,
                            IsActive = excluded.IsActive,
                            HeatProduction = excluded.HeatProduction,
                            Color = excluded.Color;";

                        using (var cmd = new SqliteCommand(insertMachineQuery, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddWithValue("@machineId", machineId);
                            cmd.Parameters.AddWithValue("@presetId", presetId);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@imageSource", imageSource);
                            cmd.Parameters.AddWithValue("@maxHeat", maxHeat);
                            cmd.Parameters.AddWithValue("@maxElectricity", maxElectricity);
                            cmd.Parameters.AddWithValue("@productionCosts", productionCosts);
                            cmd.Parameters.AddWithValue("@emissions", emissions);
                            cmd.Parameters.AddWithValue("@gasConsumption", gasConsumption);
                            cmd.Parameters.AddWithValue("@oilConsumption", oilConsumption);
                            cmd.Parameters.AddWithValue("@isActive", isActive);
                            cmd.Parameters.AddWithValue("@heatProduction", heatProduction);
                            cmd.Parameters.AddWithValue("@color", color);

                            cmd.ExecuteNonQuery();
                            Debug.WriteLine($"Machine with Id {machineId} inserted/updated successfully.");
                        }
                    }

                    // Commit the transaction
                    Debug.WriteLine("Committing transaction...");
                    transaction.Commit();
                    Debug.WriteLine("Transaction committed successfully.");
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of an error
                    Debug.WriteLine($"Error during RestoreDefaults: {ex.Message}");
                    transaction.Rollback();
                    Debug.WriteLine("Transaction rolled back.");
                }
            }

            // Re-enable foreign key enforcement
            Debug.WriteLine("Re-enabling foreign key enforcement...");
            using (var enableForeignKeysCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", conn))
            {
                enableForeignKeysCmd.ExecuteNonQuery();
                Debug.WriteLine("Foreign key enforcement re-enabled.");
            }
        }

        // Refresh in-memory data
        Debug.WriteLine("Refreshing in-memory data...");
        RefreshAssets();
        Debug.WriteLine("RestoreDefaults completed.");
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


    private bool _isInternalUpdate;
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
    public void SetIsSelectedInternal(bool value)
    {
        _isInternalUpdate = true;
        IsPresetSelected = value;
        _isInternalUpdate = false;
    }


    partial void OnIsPresetSelectedChanged(bool value)
    {
        if (!_isInternalUpdate && value) // Only trigger if not an internal update and value is true
        {
            _selectPresetAction?.Invoke(this);
        }
    }

    public void UpdateSelectionForMachine(string machineName)
    {
        IsSelected = Machines.Contains(machineName);
    }
}

public partial class AssetModel : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private int _presetId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string imageSource = string.Empty;
    [ObservableProperty] private Bitmap? _imageFromBinding;
    [ObservableProperty] private double maxHeat;
    [ObservableProperty] private double productionCosts;
    [ObservableProperty] private double emissions;
    [ObservableProperty] private double gasConsumption;
    [ObservableProperty] private double oilConsumption;
    [ObservableProperty] private double maxElectricity;
    [ObservableProperty] private ICommand? deleteMachineCommand;
    [ObservableProperty] private string originalName = string.Empty;
    [ObservableProperty] public double netCost;
    [ObservableProperty] private string color;
    public ObservableCollection<Preset> AvailablePresets { get; set; } = new();

    public bool ConsumesElectricity => MaxElectricity < 0;
    public bool ProducesElectricity => MaxElectricity > 0;
    public double CostPerMW => ProductionCosts;
    public double EmissionsPerMW => MaxHeat > 0 ? Emissions / MaxHeat : 0;

    [ObservableProperty]
    private ObservableCollection<PresetSelectionItem> _presetSelections = new();

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private double heatProduction;

    [ObservableProperty]
    private ObservableCollection<AssetModel> machineModels = new();

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