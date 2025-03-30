using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

public class DatabaseHandler
{
    private readonly string dbPath = "Data Source=heat_optimization.db;Version=3;";

    public void InitializeDatabase()
    {
        using (var conn = new SQLiteConnection(dbPath))
        {
            conn.Open();
            
            // Create tables with proper schema if they don't exist
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // SDM table matching your actual schema
                    var createSDMTable = @"CREATE TABLE IF NOT EXISTS SDM (
                                        [Time From (Winter)] TEXT NOT NULL,
                                        [Time To (Winter)] TEXT NOT NULL,
                                        [Heat Demand (Winter)] REAL NOT NULL,
                                        [Electricity Price (Winter)] REAL,
                                        [Time From (Summer)] TEXT,
                                        [Time To (Summer)] TEXT,
                                        [Heat Demand (Summer)] REAL,
                                        [Electricity Price (Summer)] REAL)";

                    using (var cmd = new SQLiteCommand(createSDMTable, conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public List<(DateTime timestamp, double value)> GetWinterHeatDemandData()
    {
        var data = new List<(DateTime timestamp, double value)>();
        
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                Debug.WriteLine("Database connection opened successfully");
                
                const string selectQuery = @"SELECT 
                                          [Time From (Winter)] AS Timestamp,
                                          [Heat Demand (Winter)] AS HeatDemand
                                          FROM SDM
                                          WHERE [Time From (Winter)] IS NOT NULL
                                          AND [Heat Demand (Winter)] IS NOT NULL
                                          ORDER BY [Time From (Winter)]";

                using (var cmd = new SQLiteCommand(selectQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Debug.WriteLine("Executing query...");
                    int pointCount = 0;
                    
                    while (reader.Read())
                    {
                        try
                        {
                            var dateStr = reader["Timestamp"].ToString();
                            var value = Convert.ToDouble(reader["HeatDemand"]);
                            
                            if (DateTime.TryParse(dateStr, out DateTime timestamp))
                            {
                                data.Add((timestamp, value));
                                pointCount++;
                                
                                // Display first 5 and last 5 points for verification
                                if (pointCount <= 5 || pointCount >= data.Count - 5)
                                {
                                    Debug.WriteLine($"Point {pointCount}: {timestamp} = {value} MWh");
                                }
                                else if (pointCount == 6)
                                {
                                    Debug.WriteLine($"... (showing first/last 5 points)");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to parse timestamp: {dateStr}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing row: {ex.Message}");
                        }
                    }
                    
                    Debug.WriteLine($"Total points retrieved: {pointCount}");
                    
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database error: {ex.Message}");
        }
        
        return data;
    }
}