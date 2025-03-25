using System;
using System.Collections.Generic;
using System.Data.SQLite;

public class DatabaseHandler
{
    private string dbPath = "Data Source=heat_optimization.db;Version=3;";

    public void InitializeDatabase()
    {
        using (var conn = new SQLiteConnection(dbPath))
        {
            conn.Open();
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS sensor_data (
                                        id INTEGER PRIMARY KEY AUTOINCREMENT, 
                                        timestamp DATETIME DEFAULT CURRENT_TIMESTAMP, 
                                        value REAL)";
            using (var cmd = new SQLiteCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void InsertData(double value)
    {
        using (var conn = new SQLiteConnection(dbPath))
        {
            conn.Open();
            string insertQuery = "INSERT INTO sensor_data (value) VALUES (@value)";
            using (var cmd = new SQLiteCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@value", value);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<(DateTime timestamp, double value)> GetData()
    {
        var data = new List<(DateTime timestamp, double value)>();
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                Console.WriteLine("Database connection opened successfully");

                
                // Column 1: Winter period
                // Column 2: Time from (DKK local time)
                // Column 3: Time to (DKK local time)
                // Column 4: Heat Demand (MWh)
                string selectQuery = "SELECT * FROM SDM";
                using (var cmd = new SQLiteCommand(selectQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var dateStr = reader.GetString(1);  // "Time from" column
                            var valueStr = reader.GetString(3); // Get as string first
                            
                            DateTime timestamp;
                            double value;
                            
                            if (DateTime.TryParse(dateStr, out timestamp) && 
                                double.TryParse(valueStr, out value))
                            {
                                data.Add((timestamp, value));
                                Console.WriteLine($"Added point: {timestamp}, {value}");
                            }
                            else
                            {
                                Console.WriteLine($"Failed to parse: date={dateStr}, value={valueStr}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading row: {ex.Message}");
                        }
                    }
                }
            }
            
            // Debug output
            Console.WriteLine($"Retrieved {data.Count} data points");
            if (data.Count > 0)
            {
                Console.WriteLine($"First point: {data[0].timestamp}, {data[0].value}");
                Console.WriteLine($"Last point: {data[data.Count-1].timestamp}, {data[data.Count-1].value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return data;
    }
}
