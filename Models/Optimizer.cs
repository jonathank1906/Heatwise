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
        Debug.WriteLine($"Optimizing with {_assetManager.CurrentAssets.Count} assets in current scenario");

        LogOptimizationStart(optimisationMode, heatDemandIntervals.Count);

        // Pre-load electricity price data
        var isWinter = heatDemandIntervals.First().timestamp.Month == 12 || heatDemandIntervals.First().timestamp.Month <= 2;
        var electricityPriceData = isWinter
            ? _sourceDataManager.GetWinterElectricityPriceData()
            : _sourceDataManager.GetSummerElectricityPriceData();

        var results = heatDemandIntervals
            .SelectMany(interval => ProcessInterval(interval, optimisationMode, electricityPriceData))
            .ToList();

        LogOptimizationCompletion(results.Count);

        return results;
    }

    private IEnumerable<HeatProductionResult> ProcessInterval(
        (DateTime timestamp, double heatDemand) interval,
        OptimisationMode optimisationMode,
        List<(DateTime timestamp, double value)> electricityPriceData)
    {
        var (timestamp, heatDemand) = interval;

        // Find the closest electricity price for the given timestamp
        var electricityPrice = electricityPriceData
            .OrderBy(d => Math.Abs((d.timestamp - timestamp).Ticks))
            .FirstOrDefault().value;

        return ProcessTimeInterval(
            timestamp,
            heatDemand,
            _assetManager.CurrentAssets,
            optimisationMode,
            electricityPrice);
    }
    private void LogOptimizationStart(OptimisationMode optimisationMode, int intervalCount)
    {
        Debug.WriteLine($"\n=== Starting optimization ===");
        Debug.WriteLine($"Mode: {optimisationMode}");
        Debug.WriteLine($"Time intervals: {intervalCount}");
        Debug.WriteLine($"Available assets: {_assetManager.CurrentAssets.Count}");
        LogCurrentAssets();
    }

    private void LogOptimizationCompletion(int resultCount)
    {
        Debug.WriteLine($"\n=== Optimization completed ===");
        Debug.WriteLine($"Total results: {resultCount}");
    }

    private List<HeatProductionResult> ProcessTimeInterval(
      DateTime timestamp,
      double heatDemand,
      List<AssetModel> assets,
      OptimisationMode optimisationMode,
      double electricityPrice)
    {
        var results = new List<HeatProductionResult>();
        double remainingDemand = heatDemand;
        double totalCost = 0;
        double totalEmissions = 0;

        Debug.WriteLine($"\nProcessing interval: {timestamp}");
        Debug.WriteLine($"Initial demand: {heatDemand} MW");

        // Filter assets to include only active ones
        var activeAssets = assets.Where(a => a.IsActive).ToList();

        // Prioritize assets based on the optimization mode
        var prioritizedAssets = optimisationMode switch
        {
            OptimisationMode.Cost => activeAssets
                .Where(a => a.HeatProduction > 0)
                .OrderBy(a => a.CostPerMW + (a.IsElectricBoiler ? electricityPrice : 0)) // Include electricity price for consumers
                .ToList(),

            OptimisationMode.CO2 => activeAssets
                .Where(a => a.HeatProduction > 0)
                .OrderBy(a => a.EmissionsPerMW)
                .ToList(),

            _ => throw new ArgumentOutOfRangeException(nameof(optimisationMode))
        };

        foreach (var asset in prioritizedAssets)
        {
            if (remainingDemand <= 0) break;

            double allocation = Math.Min(asset.HeatProduction, remainingDemand);
            remainingDemand -= allocation;

            double productionCost = allocation * asset.CostPerMW;
            double electricityProfitOrExpense = 0;

            // Adjust cost for electricity producers or consumers
            if (asset.IsElectricBoiler)
            {
                electricityProfitOrExpense = allocation * electricityPrice; // Expense for consuming electricity
                productionCost += electricityProfitOrExpense;
            }
            else if (asset.IsGenerator)
            {
                electricityProfitOrExpense = -allocation * electricityPrice; // Profit for producing electricity
                productionCost += electricityProfitOrExpense;
            }

            var result = new HeatProductionResult
            {
                AssetName = asset.Name,
                HeatProduced = allocation,
                ProductionCost = productionCost,
                Emissions = allocation * asset.EmissionsPerMW,
                Timestamp = timestamp,
                PresetId = asset.Id
            };

            totalCost += result.ProductionCost;
            totalEmissions += result.Emissions;
            results.Add(result);

            Debug.WriteLine($"- Allocated {allocation} MW from {asset.Name} " +
                            $"(Cost: {result.ProductionCost:C}, Emissions: {result.Emissions} kg, Electricity Impact: {electricityProfitOrExpense:C}, PresetId: {result.PresetId})");
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
            Timestamp = timestamp,
            PresetId = 0 // Use 0 or another value to indicate this is a summary
        });

        return results;
    }
}