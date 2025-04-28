using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

    private readonly string dbPath = "Data Source=Data/heat_optimization.db;Version=3;";

    public List<(DateTime timestamp, double value)> GetData(DataType dataType)
    {
        var data = new List<(DateTime timestamp, double value)>();
        
        try
        {
            using (var conn = new SQLiteConnection(dbPath))
            {
                conn.Open();
                Debug.WriteLine("Database connection opened successfully");
                
                // Determine which columns to select based on the dataType
                string timeColumn, valueColumn;
                
                switch (dataType)
                {
                    case DataType.WinterHeatDemand:
                        timeColumn = "[Time From (Winter)]";
                        valueColumn = "[Heat Demand (Winter)]";
                        break;
                    case DataType.SummerHeatDemand:
                        timeColumn = "[Time From (Summer)]";
                        valueColumn = "[Heat Demand (Summer)]";
                        break;
                    case DataType.WinterElectricityPrice:
                        timeColumn = "[Time From (Winter)]";
                        valueColumn = "[Electricity Price (Winter)]";
                        break;
                    case DataType.SummerElectricityPrice:
                        timeColumn = "[Time From (Summer)]";
                        valueColumn = "[Electricity Price (Summer)]";
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

                using (var cmd = new SQLiteCommand(selectQuery, conn))
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

    // You can keep these convenience methods for backward compatibility
    public List<(DateTime timestamp, double value)> GetWinterHeatDemandData()
    {
        return GetData(DataType.WinterHeatDemand);
    }

    public List<(DateTime timestamp, double value)> GetSummerHeatDemandData()
    {
        return GetData(DataType.SummerHeatDemand);
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