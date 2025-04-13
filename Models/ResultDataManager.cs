using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace Sem2Proj.Models;

public class ResultDataManager
{
    private readonly string dbPath = "Data Source=Data/heat_optimization.db;Version=3;";

    public void SaveResultsToDatabase(List<HeatProductionResult> results)
    {
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                Debug.WriteLine("Connected to database for writing results.");

                using (var checkCmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='RDM';", conn))
                {
                    var result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        Debug.WriteLine("⚠️ RDM table does NOT exist in this database!");
                    }
                    else
                    {
                        Debug.WriteLine("✅ RDM table found in database.");
                    }
                }


                using (var transaction = conn.BeginTransaction())
                {
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
                    Debug.WriteLine("Results successfully saved to RDM table.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving results to DB: {ex.Message}");
        }
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