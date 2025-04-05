using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sem2Proj.Models;

public class Optimizer
{
    private readonly AssetManager _assetManager;
    private readonly SourceDataManager _sourceDataManager;
    
    public Optimizer(AssetManager assetManager, SourceDataManager sourceDataManager)
    {
        _assetManager = assetManager;
        _sourceDataManager = sourceDataManager;

        Debug.WriteLine($"Optimizer initialized with scenario: {_assetManager.CurrentScenarioName}");
        LogCurrentAssets();
    }

    private void LogCurrentAssets()
    {
        Debug.WriteLine($"Current Assets ({_assetManager.CurrentAssets.Count}):");
        foreach (var asset in _assetManager.CurrentAssets)
        {
            Debug.WriteLine($"[{asset.Name}] " +
                          $"MaxHeat: {asset.MaxHeat} MW, " +
                          $"Cost: {asset.ProductionCosts:C}/h, " +
                          $"Emissions: {asset.Emissions} kg/MWh, " +
                          $"Electricity: {(asset.IsElectricBoiler ? "Consumer" : asset.IsGenerator ? "Producer" : "None")}");
        }
    }

    public List<HeatProductionResult> CalculateOptimalHeatProduction(
        List<(DateTime timestamp, double heatDemand)> heatDemandIntervals, 
        OptimisationMode optimisationMode)
    {
        var results = new List<HeatProductionResult>();
        var currentAssets = _assetManager.CurrentAssets;
        
        Debug.WriteLine($"\n=== Starting optimization ===");
        Debug.WriteLine($"Mode: {optimisationMode}");
        Debug.WriteLine($"Time intervals: {heatDemandIntervals.Count}");
        Debug.WriteLine($"Available assets: {currentAssets.Count}");

        foreach (var (timestamp, heatDemand) in heatDemandIntervals)
        {
            var intervalResults = ProcessTimeInterval(
                timestamp, 
                heatDemand, 
                currentAssets, 
                optimisationMode);
            
            results.AddRange(intervalResults);
        }

        Debug.WriteLine($"\n=== Optimization completed ===");
        Debug.WriteLine($"Total results: {results.Count}");
        
        return results;
    }

    private List<HeatProductionResult> ProcessTimeInterval(
        DateTime timestamp,
        double heatDemand,
        List<AssetModel> assets,
        OptimisationMode optimisationMode)
    {
        var results = new List<HeatProductionResult>();
        double remainingDemand = heatDemand;
        double totalCost = 0;
        double totalEmissions = 0;

        Debug.WriteLine($"\nProcessing interval: {timestamp}");
        Debug.WriteLine($"Initial demand: {heatDemand} MW");

        var prioritizedAssets = optimisationMode switch
        {
            OptimisationMode.Cost => assets
                .Where(a => a.MaxHeat > 0)
                .OrderBy(a => a.CostPerMW)
                .ToList(),
            
            OptimisationMode.CO2 => assets
                .Where(a => a.MaxHeat > 0)
                .OrderBy(a => a.EmissionsPerMW)
                .ToList(),
            
            _ => throw new ArgumentOutOfRangeException(nameof(optimisationMode))
        };

        foreach (var asset in prioritizedAssets)
        {
            if (remainingDemand <= 0) break;

            double allocation = Math.Min(asset.MaxHeat, remainingDemand);
            remainingDemand -= allocation;

            var result = new HeatProductionResult
            {
                AssetName = asset.Name,
                HeatProduced = allocation,
                ProductionCost = allocation * asset.CostPerMW,
                Emissions = allocation * asset.EmissionsPerMW,
                Timestamp = timestamp
            };

            totalCost += result.ProductionCost;
            totalEmissions += result.Emissions;
            results.Add(result);

            Debug.WriteLine($"- Allocated {allocation} MW from {asset.Name} " +
                          $"(Cost: {result.ProductionCost:C}, Emissions: {result.Emissions} kg)");
        }

        if (remainingDemand > 0)
        {
            Debug.WriteLine($"WARNING: Unmet demand of {remainingDemand} MW");
        }

        // Add interval summary
        results.Add(new HeatProductionResult
        {
            AssetName = "Interval Summary",
            HeatProduced = heatDemand - remainingDemand,
            ProductionCost = totalCost,
            Emissions = totalEmissions,
            Timestamp = timestamp
        });

        return results;
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