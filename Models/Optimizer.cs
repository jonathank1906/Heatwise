using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Heatwise.Models;

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

    public List<HeatProductionResult> CalculateOptimalHeatProduction(
       List<(DateTime timestamp, double heatDemand)> heatDemandIntervals,
       OptimisationMode optimisationMode,
       List<(DateTime timestamp, double value)> electricityPriceData)
    {
        Debug.WriteLine($"Optimizing with {_assetManager.CurrentAssets.Count} assets in current scenario");
        LogOptimizationStart(optimisationMode, heatDemandIntervals.Count);

        var results = heatDemandIntervals
            .SelectMany(interval => ProcessInterval(interval, optimisationMode, electricityPriceData))
            .ToList();

        LogOptimizationCompletion(results.Count);
        return results;
    }

    private double CalculateNetCost(AssetModel asset, double electricityPrice)
    {
        double netCost;

        if (asset.ProducesElectricity)
        {
            double electricityPerMWhHeat = asset.MaxElectricity / asset.MaxHeat; // 0.743 
            netCost = asset.CostPerMW - (electricityPrice * electricityPerMWhHeat);

            Debug.WriteLine($"[Net Cost Calculation] Asset: {asset.Name}, Produces Electricity");
            Debug.WriteLine($"  CostPerMW: {asset.CostPerMW}, ElectricityPrice: {electricityPrice}, ElectricityPerMWhHeat: {electricityPerMWhHeat}");
            Debug.WriteLine($"  NetCost: {netCost:F2} DKK/MWh");
        }
        else if (asset.ConsumesElectricity)
        {
            double electricityPerMWhHeat = Math.Abs(asset.MaxElectricity / asset.MaxHeat); // 1
            netCost = asset.CostPerMW + electricityPrice * electricityPerMWhHeat;

            Debug.WriteLine($"[Net Cost Calculation] Asset: {asset.Name}, Consumes Electricity");
            Debug.WriteLine($"  CostPerMW: {asset.CostPerMW}, ElectricityPrice: {electricityPrice}");
            Debug.WriteLine($"  NetCost: {netCost:F2} DKK/MWh");
        }
        else
        {
            netCost = asset.CostPerMW;

            Debug.WriteLine($"[Net Cost Calculation] Asset: {asset.Name}, No Electricity Interaction");
            Debug.WriteLine($"  CostPerMW: {asset.CostPerMW}");
            Debug.WriteLine($"  NetCost: {netCost:F2} DKK/MWh");
        }

        return netCost;
    }
    private List<AssetModel> SortByPriority(List<AssetModel> assets, double electricityPrice, OptimisationMode optimisationMode)
    {
        if (optimisationMode == OptimisationMode.CO2)
        {
            return assets
                .Where(a => a.IsActive && a.HeatProduction > 0)
                .OrderBy(a => a.EmissionsPerMW) // Prioritize lower emissions
                .ToList();
        }

        // Default to cost optimization
        return assets
            .Where(a => a.IsActive && a.HeatProduction > 0)
            .OrderBy(a => CalculateNetCost(a, electricityPrice))
            .ToList();
    }

    private List<HeatProductionResult> ProcessInterval((DateTime timestamp, double heatDemand) interval, OptimisationMode optimisationMode, List<(DateTime timestamp, double value)> electricityPriceData)
    {
        var timestamp = interval.timestamp;
        var heatDemand = interval.heatDemand;

        // Find the electricity price for the current timestamp
        var electricityPrice = electricityPriceData
            .FirstOrDefault(p => p.timestamp == timestamp).value;

        if (electricityPrice == default)
        {
            Debug.WriteLine($"WARNING: No electricity price found for timestamp {timestamp}. Defaulting to 0.");
            electricityPrice = 0.0;
        }

        // Debug statements
        Debug.WriteLine($"\nProcessing interval: {timestamp}");
        Debug.WriteLine($"Initial demand: {heatDemand} MW");
        Debug.WriteLine($"Electricity price: {electricityPrice} DKK/MWh");

        // Sort assets by priority based on the electricity price
        var prioritizedAssets = SortByPriority(_assetManager.CurrentAssets, electricityPrice, optimisationMode);

        // Allocate heat demand to the prioritized assets
        return AllocateHeat(timestamp, heatDemand, prioritizedAssets, electricityPrice);
    }

    private List<HeatProductionResult> AllocateHeat(DateTime timestamp, double heatDemand, List<AssetModel> prioritizedAssets, double electricityPrice)
    {
        var results = new List<HeatProductionResult>();
        double remainingDemand = heatDemand;

        foreach (var asset in prioritizedAssets)
        {
            if (remainingDemand <= 0) break;

            double allocation = Math.Min(asset.HeatProduction, remainingDemand);
            remainingDemand -= allocation;

            double netCostPerMWh = CalculateNetCost(asset, electricityPrice);
            double productionCost = allocation * netCostPerMWh;

            // Calculate electricity consumption or production
            double electricityConsumption = 0;
            double electricityProduction = 0;
            if (asset.ConsumesElectricity)
            {
                electricityConsumption = allocation * Math.Abs(asset.MaxElectricity / asset.MaxHeat);
            }
            else if (asset.ProducesElectricity)
            {
                electricityProduction = allocation * (asset.MaxElectricity / asset.MaxHeat);
            }

            // Calculate oil and gas consumption
            double oilConsumption = allocation * asset.OilConsumption;
            double gasConsumption = allocation * asset.GasConsumption;

            results.Add(new HeatProductionResult
            {
                AssetName = asset.Name,
                HeatProduced = allocation,
                ProductionCost = productionCost,
                Emissions = allocation * asset.EmissionsPerMW,
                Timestamp = timestamp,
                PresetId = asset.PresetId,
                ElectricityConsumption = electricityConsumption,
                ElectricityProduction = electricityProduction,
                OilConsumption = oilConsumption, // Added
                GasConsumption = gasConsumption  // Added
            });

            Debug.WriteLine($"- Allocated {allocation} MW from {asset.Name} (Net Cost: {netCostPerMWh:F2} DKK/MWh, Total Cost: {productionCost:F2} DKK, Electricity Consumption: {electricityConsumption:F2} MWh, Oil Consumption: {oilConsumption:F2} MWh, Gas Consumption: {gasConsumption:F2} MWh)");
        }

        if (remainingDemand > 0)
        {
            // Debug.WriteLine($"WARNING: Unmet demand of {remainingDemand} MW");

            results.Add(new HeatProductionResult
            {
                AssetName = "Unmet Demand",
                HeatProduced = remainingDemand,
                ProductionCost = 0,
                Emissions = 0,
                Timestamp = timestamp,
                PresetId = -1,
                ElectricityConsumption = 0,
                ElectricityProduction = 0,
                OilConsumption = 0, // Added
                GasConsumption = 0  // Added
            });
        }

        return results;
    }

    private void LogCurrentAssets()
    {
        Debug.WriteLine($"Current Assets ({_assetManager.CurrentAssets.Count}):");
        foreach (var asset in _assetManager.CurrentAssets)
        {
            Debug.WriteLine($"[{asset.Name}] " +
                            $"MaxHeat: {asset.MaxHeat} MW, " +
                            $"Cost: {asset.ProductionCosts:C}/MWh, " +
                            $"Emissions: {asset.Emissions} kg/MWh, " +
                            $"Electricity: {(asset.ConsumesElectricity ? "Consumer" : asset.ProducesElectricity ? "Producer" : "None")}");
        }
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
}