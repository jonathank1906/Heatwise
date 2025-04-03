using System;
using System.Collections.Generic;
using System.Linq;

namespace Sem2Proj.Models;

public class Optimizer()
{
    private readonly List<AssetModel> _assets; // List of assets to be optimized
    private SourceDataManager SourceDataManager = new SourceDataManager();
    private ResultDataManager ResultDataManager = new ResultDataManager();

    //     // Step 1: Recalculate production costs for electric boilers based on electricity price
    //     public void RecalculateProductionCosts(double electricityPrice)
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
    public List<HeatProductionResult> CalculateOptimalHeatProduction(double heatDemand, OptimisationMode optimisationMode)
    {
        // Step 2.1: Sort assets based on the optimization criteria
       var sortedAssets = optimisationMode == OptimisationMode.CO2
        ? _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.Emissions).ToList()
        : _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.ProductionCosts / a.MaxHeat).ToList();

        // Step 2.2: Initialize variables (tracking total production cost, emissions, and remaining heat demand)
        List<HeatProductionResult> results = new();
        double remainingHeatDemand = heatDemand;
        double totalProductionCost = 0;
        double totalEmissions = 0;

        // Step 3: Allocate heat production
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
                Emissions = heatProduced * asset.Emissions
            });
        }

        // Step 3.1: Log a message if demand is not met
        if (remainingHeatDemand > 0)
        {
            Console.WriteLine($"Warning: Heat demand of {heatDemand} could not be fully met. Remaining demand: {remainingHeatDemand}");
        }

        // Step 4: Add summary data
        results.Add(new HeatProductionResult
        {
            AssetId = "Summary",
            HeatProduced = heatDemand - remainingHeatDemand,
            ProductionCost = totalProductionCost,
            Emissions = totalEmissions
        });

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