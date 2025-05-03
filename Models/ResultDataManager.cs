using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;

namespace Sem2Proj.Models;

public class ResultDataManager
{
    private readonly string dbPath = "Data Source=Data/heat_optimization.db;Version=3;";

    // Clears ALL data from the RDM table before saving new results
    public void ClearAllResults()
    {
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM RDM", conn))
                {
                    cmd.ExecuteNonQuery();
                    Debug.WriteLine("RDM table cleared successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error clearing RDM table: {ex.Message}");
        }
    }

    // Saves new results (after clearing old data)
    public void SaveResultsToDatabase(List<HeatProductionResult> results)
    {
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Clear old data
                    new SQLiteCommand("DELETE FROM RDM", conn).ExecuteNonQuery();

                    // 2. Insert new results with ElectricityConsumption
                    string insertQuery = @"
                    INSERT INTO RDM 
                    (Timestamp, [Asset Name], [Produced Heat], [Production Cost], [Emissions], [PresetId], [Electricity Consumption], [Electricity Production], [Oil Consumption], [Gas Consumption])
                    VALUES 
                    (@Timestamp, @AssetName, @HeatProduced, @ProductionCost, @Emissions, @PresetId, @ElectricityConsumption, @ElectricityProduction, @OilConsumption, @GasConsumption)";

                    using (var cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        foreach (var result in results)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Timestamp", result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@AssetName", result.AssetName);
                            cmd.Parameters.AddWithValue("@HeatProduced", result.HeatProduced);
                            cmd.Parameters.AddWithValue("@ProductionCost", result.ProductionCost);
                            cmd.Parameters.AddWithValue("@Emissions", result.Emissions);
                            cmd.Parameters.AddWithValue("@PresetId", result.PresetId);
                            cmd.Parameters.AddWithValue("@ElectricityConsumption", result.ElectricityConsumption);
                            cmd.Parameters.AddWithValue("@ElectricityProduction", result.ElectricityProduction);
                            cmd.Parameters.AddWithValue("@OilConsumption", result.OilConsumption);
                            cmd.Parameters.AddWithValue("@GasConsumption", result.GasConsumption);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    Debug.WriteLine("New results with ElectricityConsumption saved to RDM after clearing old data.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving results to DB: {ex.Message}");
        }
    }

    public List<HeatProductionResult> GetLatestResults()
    {
        var results = new List<HeatProductionResult>();
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                string query = @"
                SELECT Timestamp, [Asset Name], [Produced Heat], [Production Cost], [Emissions], [PresetId], [Electricity Consumption], [Electricity Production], [Oil Consumption], [Gas Consumption]
                FROM RDM
                ORDER BY Timestamp";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new HeatProductionResult
                        {
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()!),
                            AssetName = reader["Asset Name"].ToString()!,
                            HeatProduced = Convert.ToDouble(reader["Produced Heat"]),
                            ProductionCost = Convert.ToDouble(reader["Production Cost"]),
                            Emissions = Convert.ToDouble(reader["Emissions"]),
                            PresetId = Convert.ToInt32(reader["PresetId"]),
                            ElectricityConsumption = Convert.ToDouble(reader["Electricity Consumption"]),
                            ElectricityProduction = Convert.ToDouble(reader["Electricity Production"]),
                            OilConsumption = Convert.ToDouble(reader["Oil Consumption"]),
                            GasConsumption = Convert.ToDouble(reader["Gas Consumption"])
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading results from RDM: {ex.Message}");
        }
        return results;
    }

    public void ExportToCsv(string filePath)
    {
        try
        {
            var results = GetLatestResults();

            using (var writer = new StreamWriter(filePath))
            {
                // Write CSV header
                writer.WriteLine("Timestamp,Asset Name,Produced Heat (MW),Production Cost (DKK),Emissions (kg CO2),Electricity Consumption (MWh),Electricity Production (MWh)");

                // Write data rows
                foreach (var result in results)
                {
                    writer.WriteLine(
                    $"{result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{EscapeCsvField(result.AssetName)}," +
                    $"{result.HeatProduced.ToString(CultureInfo.InvariantCulture)}," +
                    $"{result.ProductionCost.ToString(CultureInfo.InvariantCulture)}," +
                    $"{result.Emissions.ToString(CultureInfo.InvariantCulture)}," +
                    $"{result.ElectricityConsumption.ToString(CultureInfo.InvariantCulture)}," +
                    $"{result.ElectricityProduction.ToString(CultureInfo.InvariantCulture)}"
                );
                }
            }

            Debug.WriteLine($"Successfully exported results to {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exporting to CSV: {ex.Message}");
            throw; // Re-throw to handle in UI
        }
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
public class HeatProductionResult
{
    public string AssetName { get; set; } = string.Empty;
    public double HeatProduced { get; set; }
    public double ProductionCost { get; set; }
    public double Emissions { get; set; }
    public DateTime Timestamp { get; set; }
    public int PresetId { get; set; }
    public double ElectricityConsumption { get; set; }
    public double ElectricityProduction { get; set; }
    public double OilConsumption { get; set; }
    public double GasConsumption { get; set; }
}
public enum OptimisationMode
{
    CO2,
    Cost
}