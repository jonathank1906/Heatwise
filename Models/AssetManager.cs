using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sem2Proj.Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public partial class AssetModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string imageSource;

    [ObservableProperty]
    private double maxHeat;

    [ObservableProperty]
    private double productionCosts;

    [ObservableProperty]
    private double emissions;

    [ObservableProperty]
    private double gasConsumption;

    [ObservableProperty]
    private double oilConsumption;

    [ObservableProperty]
    private double maxElectricity;
}


public class AssetManager
{
    public List<AssetModel> Assets { get; set; } = new();

    public AssetManager()
    {
        Assets = LoadAssetsFromJson();
    }

    public List<AssetModel> LoadAssetsFromJson()
    {
        // Get the base directory (where the app is running)
        string basePath = AppDomain.CurrentDomain.BaseDirectory;

        // Construct the full path to the JSON file in the root directory
        string jsonFilePath = Path.Combine(basePath, "Data/HeatProductionUnits.json");

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"File not found: {jsonFilePath}");
            return new List<AssetModel>();
        }
        Debug.WriteLine($"Loading assets from: {jsonFilePath}");
        string jsonString = File.ReadAllText(jsonFilePath);

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            if (root.TryGetProperty("Assets", out var assetsElement))
            {
                var assets = JsonSerializer.Deserialize<List<AssetModel>>(assetsElement.GetRawText()) ?? new List<AssetModel>();
                Console.WriteLine($"Loaded {assets.Count} assets.");
                foreach (var asset in assets)
                {
                    Console.WriteLine($"Asset: {asset.Name}, MaxHeat: {asset.MaxHeat}, ProductionCosts: {asset.ProductionCosts}, Emissions: {asset.Emissions}");
                }
                return assets;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
        }

        return new List<AssetModel>();
    }
}

public class HeatingGrid
{
    public string Architecture { get; set; }
    public int CityBuildings { get; set; }
    public string CityName { get; set; }

    // Constructor for the HeatingGrid class
    public HeatingGrid(string architecture, int cityBuildings, string cityName)
    {
        Architecture = architecture;
        CityBuildings = cityBuildings;
        CityName = cityName;
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.