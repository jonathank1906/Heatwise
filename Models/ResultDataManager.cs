using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;

namespace Heatwise.Models;

public class ResultDataManager
{
    private readonly string dbPath = "Data Source=Data/heat_optimization.db;";

    public void SaveResultsToDatabase(List<HeatProductionResult> results)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();


                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Clear old data
                    new SqliteCommand("DELETE FROM RDM", conn, transaction).ExecuteNonQuery();

                    // 2. Insert new results with ElectricityConsumption
                    string insertQuery = @"
                INSERT INTO RDM 
                (Timestamp, AssetName, ProducedHeat, ProductionCost, Emissions, PresetId, ElectricityConsumption, ElectricityProduction, OilConsumption, GasConsumption)
                VALUES 
                (@Timestamp, @AssetName, @HeatProduced, @ProductionCost, @Emissions, @PresetId, @ElectricityConsumption, @ElectricityProduction, @OilConsumption, @GasConsumption)";

                    using (var cmd = new SqliteCommand(insertQuery, conn, transaction))
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
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                string query = @"
                SELECT Timestamp, AssetName, ProducedHeat, ProductionCost, Emissions, PresetId, ElectricityConsumption, ElectricityProduction, OilConsumption, GasConsumption
                FROM RDM
                ORDER BY Timestamp";
                using (var cmd = new SqliteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                    }
                    while (reader.Read())
                    {
                        results.Add(new HeatProductionResult
                        {
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()!),
                            AssetName = reader["AssetName"].ToString()!,
                            HeatProduced = Convert.ToDouble(reader["ProducedHeat"]),
                            ProductionCost = Convert.ToDouble(reader["ProductionCost"]),
                            Emissions = Convert.ToDouble(reader["Emissions"]),
                            PresetId = Convert.ToInt32(reader["PresetId"]),
                            ElectricityConsumption = Convert.ToDouble(reader["ElectricityConsumption"]),
                            ElectricityProduction = Convert.ToDouble(reader["ElectricityProduction"]),
                            OilConsumption = Convert.ToDouble(reader["OilConsumption"]),
                            GasConsumption = Convert.ToDouble(reader["GasConsumption"])
                        });
                    }
                }
            }
        }
        catch
        {

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
        }
        catch
        {

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