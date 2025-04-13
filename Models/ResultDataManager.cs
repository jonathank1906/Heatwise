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

                    // 2. Insert new results
                    string insertQuery = @"
                        INSERT INTO RDM 
                        (Timestamp, [Asset Name], [Produced Heat], [Production Cost], [Emissions])
                        VALUES 
                        (@Timestamp, @AssetName, @HeatProduced, @ProductionCost, @Emissions)";

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
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    Debug.WriteLine("New results saved to RDM after clearing old data.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving results to DB: {ex.Message}");
        }
    }

    // Fetches the ONLY results in the RDM (since we clear it every time)
    public List<HeatProductionResult> GetLatestResults()
    {
        var results = new List<HeatProductionResult>();
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                string query = @"
                    SELECT Timestamp, [Asset Name], [Produced Heat], [Production Cost], [Emissions]
                    FROM RDM
                    ORDER BY Timestamp";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new HeatProductionResult
                        {
                            Timestamp = DateTime.Parse(reader["Timestamp"].ToString()),
                            AssetName = reader["Asset Name"].ToString(),
                            HeatProduced = Convert.ToDouble(reader["Produced Heat"]),
                            ProductionCost = Convert.ToDouble(reader["Production Cost"]),
                            Emissions = Convert.ToDouble(reader["Emissions"])
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
                writer.WriteLine("Timestamp,Asset Name,Produced Heat (MW),Production Cost (DKK),Emissions (kg CO2)");
                
                // Write data rows
                foreach (var result in results)
                {
                    writer.WriteLine(
                        $"{result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}," +
                        $"{EscapeCsvField(result.AssetName)}," +
                        $"{result.HeatProduced.ToString(CultureInfo.InvariantCulture)}," +
                        $"{result.ProductionCost.ToString(CultureInfo.InvariantCulture)}," +
                        $"{result.Emissions.ToString(CultureInfo.InvariantCulture)}"
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
}

public enum OptimisationMode
{
    CO2,
    Cost
}