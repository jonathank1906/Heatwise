using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sem2Proj.Models
{
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

    public class Preset
    {
        public string Name { get; set; }
        public List<string> Machines { get; set; }
    }

    public class AssetData
    {
        public List<AssetModel> Assets { get; set; }
        public List<Preset> Presets { get; set; }
    }

    public class AssetManager
    {
        public List<AssetModel> Assets { get; set; }
        public List<Preset> Presets { get; set; }

        public AssetManager()
        {
            LoadAssetsFromJson();
        }

        public List<AssetModel> LoadAssetsFromJson()
        {
            // Get the base directory (where the app is running)
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Construct the full path to the JSON file in the root directory
            string jsonFilePath = Path.Combine(basePath, "HeatProductionUnits.json");

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"File not found: {jsonFilePath}");
                return new List<AssetModel>();
            }

            string jsonString = File.ReadAllText(jsonFilePath);
            //.WriteLine($"JSON content: {jsonString}");

            AssetData assetData;
            try
            {
                assetData = JsonSerializer.Deserialize<AssetData>(jsonString) ?? new AssetData();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                return new List<AssetModel>();
            }

            Assets = assetData.Assets ?? new List<AssetModel>();
            Presets = assetData.Presets ?? new List<Preset>();

            return Assets;
        }
    }
    #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}