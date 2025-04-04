using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sem2Proj.Models;

public class Optimizer
{
    private readonly List<AssetModel> _assets; // List of assets to be optimized
    private readonly SourceDataManager _sourceDataManager;
    private ResultDataManager ResultDataManager = new ResultDataManager();

    public Optimizer(AssetManager assetManager, SourceDataManager sourceDataManager)
    {
        _assets = assetManager.Assets;
        _sourceDataManager = sourceDataManager;

        Console.WriteLine($"Optimizer initialized with {_assets.Count} assets.");
    foreach (var asset in _assets)
    {
        Console.WriteLine($"Asset: {asset.Name}, MaxHeat: {asset.MaxHeat}, ProductionCosts: {asset.ProductionCosts}, Emissions: {asset.Emissions}");
    }
    }

    //     // Step 1: Calculate production costs for electric boilers based on electricity price
    //     public void CalculateProductionCosts(double electricityPrice)
    //     {
    //         foreach (var asset in _assets)
    //         {
    //             if (asset.IsElectricBoiler)
    //             {
    //                 asset.ProductionCosts = asset.ElectricityConsumption * electricityPrice;
    //             }
    //         }
    //     }

    // Step 2: Calculate optimal heat production
public List<HeatProductionResult> CalculateOptimalHeatProduction(List<(DateTime timestamp, double heatDemand)> heatDemandIntervals, OptimisationMode optimisationMode)
{
    var results = new List<HeatProductionResult>();

    Console.WriteLine($"Total assets: {_assets.Count}");
    foreach (var asset in _assets)
    {
        Console.WriteLine($"Asset: {asset.Name}, MaxHeat: {asset.MaxHeat}, ProductionCosts: {asset.ProductionCosts}, Emissions: {asset.Emissions}");
    }

    foreach (var (timestamp, heatDemand) in heatDemandIntervals)
    {
        // Step 2.1: Sort assets based on the optimization criteria
        var sortedAssets = optimisationMode == OptimisationMode.CO2
            ? _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.Emissions).ToList()
            : _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.ProductionCosts / a.MaxHeat).ToList();

        Console.WriteLine($"Filtered assets for {optimisationMode}: {sortedAssets.Count}");
        foreach (var asset in sortedAssets)
        {
            Console.WriteLine($"Asset: {asset.Name}, MaxHeat: {asset.MaxHeat}, ProductionCosts: {asset.ProductionCosts}, Emissions: {asset.Emissions}");
        }

        // Step 2.2: Initialize variables for this interval
        double remainingHeatDemand = heatDemand;
        double totalProductionCost = 0;
        double totalEmissions = 0;

        // Step 3: Allocate heat production for this interval
        foreach (var asset in sortedAssets)
        {
            if (remainingHeatDemand <= 0)
                break;

            double heatProduced = Math.Min(asset.MaxHeat, remainingHeatDemand);
            remainingHeatDemand -= heatProduced;

            totalProductionCost += heatProduced * (asset.ProductionCosts / asset.MaxHeat);
            totalEmissions += heatProduced * asset.Emissions;

            results.Add(new HeatProductionResult
            {
                AssetId = asset.Name,
                HeatProduced = heatProduced,
                ProductionCost = heatProduced * (asset.ProductionCosts / asset.MaxHeat),
                Emissions = heatProduced * asset.Emissions,
                Timestamp = timestamp // Add timestamp for this interval
            });
        }

        // Step 3.1: Log a message if demand is not met for this interval
        if (remainingHeatDemand > 0)
        {
            Console.WriteLine($"Warning: Heat demand of {heatDemand} at {timestamp} could not be fully met. Remaining demand: {remainingHeatDemand}");
        }

        // Step 4: Add summary data for this interval
        results.Add(new HeatProductionResult
        {
            AssetId = "Summary",
            HeatProduced = heatDemand - remainingHeatDemand,
            ProductionCost = totalProductionCost,
            Emissions = totalEmissions,
            Timestamp = timestamp // Add timestamp for this interval
        });
    }

    return results;
}


}

// Helper class to store the result of heat production optimization
public class HeatProductionResult
{
    public string AssetId { get; set; }
    public double HeatProduced { get; set; }
    public double ProductionCost { get; set; }
    public double Emissions { get; set; }

    public DateTime Timestamp { get; set; }
}

public enum SortBy
{
    Cost,
    CO2,
    ElectricityConsumption,
    ElectricityProduction,
    HeatCapacity,
}

public enum OptimisationMode
{
    CO2,
    Cost
}