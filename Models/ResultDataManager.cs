using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sem2Proj.Models;

namespace Sem2Proj.Models
{
    public class ResultDataManager
    {
        private readonly string _resultsFilePath = "optimization_results.json";
        
        // Save optimization result object to JSON
        public void SaveResults(object results)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_resultsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving optimization results: {ex.Message}");
            }
        }
        
        // Load optimization results from file
        public T? LoadResults<T>()
        {
            try
            {
                if (File.Exists(_resultsFilePath))
                {
                    string jsonString = File.ReadAllText(_resultsFilePath);
                    return JsonSerializer.Deserialize<T>(jsonString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading optimization results: {ex.Message}");
            }
            return default;
        }
    }
}