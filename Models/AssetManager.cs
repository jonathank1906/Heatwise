using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Sem2Proj.Models;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public class HeatAsset
{
    public string Name { get; set; } = ""; // Name of the asset
    public double MaxHeat { get; set; } //MWh
    public double ProductionCosts { get; set; } //DKK/MWh(th)
    public double CO2Emissions { get; set; } //kg/MWh(th)

    // Gas boiler
    public class GasBoiler : HeatAsset
    {
        public double GasConsumption { get; set; } // MWh(gas)/MWh(th)
    }

    // Oil boiler
    public class OilBoiler : HeatAsset
    {
        public double OilConsumption { get; set; } // MWh(oil)/MWh(th)
    }

    // Gas motor (produces heat & electricity)
    public class GasMotor : HeatAsset
    {
        public double MaxElectricity { get; set; } // in MW
        public double GasConsumption { get; set; } // MWh(gas)/MWh(th)
    }

    // Heat pump (uses electricity to generate heat)
    public class HeatPump : HeatAsset
    {
        public double MaxElectricity { get; set; } // in MW
    }
}

public class Preset
{
    public string Name { get; set; }
    public List<string> Machines { get; set; }
}

public class AssetData
{
    public List<HeatAsset.GasBoiler> GasBoilers { get; set; }
    public List<HeatAsset.OilBoiler> OilBoilers { get; set; }
    public List<HeatAsset.GasMotor> GasMotors { get; set; }
    public List<HeatAsset.HeatPump> HeatPumps { get; set; }
    public List<Preset> Presets { get; set; }
}

public class AssetManager
{
    public List<HeatAsset.GasBoiler> GasBoilers { get; set; }
    public List<HeatAsset.OilBoiler> OilBoilers { get; set; }
    public List<HeatAsset.GasMotor> GasMotors { get; set; }
    public List<HeatAsset.HeatPump> HeatPumps { get; set; }
    public List<Preset> Presets { get; set; }

    public AssetManager()
    {
        LoadAssets();
    }

    private void LoadAssets()
    {
        try
        {
            // Get the base directory (where the app is running)
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Construct the full path to the JSON file in the root directory
            string jsonFilePath = Path.Combine(basePath, "HeatProductionUnits.json");

            if (File.Exists(jsonFilePath))
            {
                string jsonString = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var assets = JsonSerializer.Deserialize<AssetData>(jsonString, options);

                if (assets != null)
                {
                    GasBoilers = assets.GasBoilers;
                    OilBoilers = assets.OilBoilers;
                    GasMotors = assets.GasMotors;
                    HeatPumps = assets.HeatPumps;
                    Presets = assets.Presets;
                    Console.WriteLine("Assets loaded successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to load assets: Deserialized object is null.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to load assets: File not found at {jsonFilePath}");
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("Failed to load assets: Error parsing JSON.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load assets: {ex.Message}");
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.