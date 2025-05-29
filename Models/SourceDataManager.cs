using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

public class SourceDataManager
{
    public enum DataType
    {
        WinterHeatDemand,
        SummerHeatDemand,
        WinterElectricityPrice,
        SummerElectricityPrice
    }

    private readonly string dbPath = "Data Source=Data/heat_optimization.db;";

    public List<(DateTime timestamp, double value)> GetData(DataType dataType)
    {
        var data = new List<(DateTime timestamp, double value)>();

        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                Debug.WriteLine("Database connection opened successfully");

                // Determine which columns to select based on the dataType
                string timeColumn, valueColumn;

                switch (dataType)
                {
                    case DataType.WinterHeatDemand:
                        timeColumn = "TimeFromWinter";
                        valueColumn = "HeatDemandWinter";
                        break;
                    case DataType.SummerHeatDemand:
                        timeColumn = "TimeFromSummer";
                        valueColumn = "HeatDemandSummer";
                        break;
                    case DataType.WinterElectricityPrice:
                        timeColumn = "TimeFromWinter";
                        valueColumn = "ElectricityPriceWinter";
                        break;
                    case DataType.SummerElectricityPrice:
                        timeColumn = "TimeFromSummer";
                        valueColumn = "ElectricityPriceSummer";
                        break;
                    default:
                        throw new ArgumentException("Invalid data type specified");
                }

                string selectQuery = $@"SELECT 
                                      {timeColumn} AS Timestamp,
                                      {valueColumn} AS Value
                                      FROM SDM
                                      WHERE {timeColumn} IS NOT NULL
                                      AND {valueColumn} IS NOT NULL
                                      ORDER BY {timeColumn}";

                using (var cmd = new SqliteCommand(selectQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Debug.WriteLine($"Executing query for {dataType}...");
                    int pointCount = 0;

                    while (reader.Read())
                    {
                        try
                        {
                            var dateStr = reader["Timestamp"].ToString();
                            var value = Convert.ToDouble(reader["Value"]);

                            if (DateTime.TryParse(dateStr, out DateTime timestamp))
                            {
                                data.Add((timestamp, value));
                                pointCount++;

                                // Display first 5 and last 5 points for verification
                                if (pointCount <= 5 || pointCount >= data.Count - 5)
                                {
                                    Debug.WriteLine($"Point {pointCount}: {timestamp} = {value}");
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

    public void SaveSetting(string key, string value)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                const string query = @"
                    INSERT INTO UserSettings (Key, Value)
                    VALUES (@key, @value)
                    ON CONFLICT(Key) DO UPDATE SET Value = @value";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving setting '{key}': {ex.Message}");
        }
    }

    public string? GetSetting(string key)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                const string query = "SELECT Value FROM UserSettings WHERE Key = @key";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving setting '{key}': {ex.Message}");
            return null;
        }
    }
    
    public List<(DateTime timestamp, double value)> GetWinterElectricityPriceData()
    {
        return GetData(DataType.WinterElectricityPrice);
    }

    public List<(DateTime timestamp, double value)> GetSummerElectricityPriceData()
    {
        return GetData(DataType.SummerElectricityPrice);
    }
}