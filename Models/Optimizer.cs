using System;
using System.Collections.Generic;
using System.Linq;
using Sem2Proj.Models;

namespace Sem2Proj.Models;

    //public class Optimizer
    //{
    //     private readonly List<AssetModel> _assets;

    //     public Optimizer(List<AssetModel> assets)
    //     {
    //         _assets = assets;
    //     }

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

    //     // Step 2: Calculate optimal heat production
    //     public List<HeatProductionResult> CalculateOptimalHeatProduction(double heatDemand, bool optimizeEmissions)
    //     {
    //         // Step 2.1: Sort assets based on the optimization criteria
    //         var sortedAssets = optimizeEmissions
    //             ? _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.EmissionsPerUnitHeat).ToList()
    //             : _assets.Where(a => a.MaxHeat > 0).OrderBy(a => a.ProductionCosts / a.MaxHeat).ToList();

    //         List<HeatProductionResult> results = new();
    //         double remainingHeatDemand = heatDemand;
    //         double totalProductionCost = 0;
    //         double totalEmissions = 0;

    //         // Step 3: Allocate heat production
    //         foreach (var asset in sortedAssets)
    //         {
    //             if (remainingHeatDemand <= 0)
    //                 break;

    //             double heatProduced = Math.Min(asset.MaxHeat, remainingHeatDemand);
    //             remainingHeatDemand -= heatProduced;

    //             totalProductionCost += heatProduced * (asset.ProductionCosts / asset.MaxHeat);
    //             totalEmissions += heatProduced * asset.EmissionsPerUnitHeat;

    //             results.Add(new HeatProductionResult
    //             {
    //                 AssetId = asset.Id,
    //                 HeatProduced = heatProduced,
    //                 ProductionCost = heatProduced * (asset.ProductionCosts / asset.MaxHeat),
    //                 Emissions = heatProduced * asset.EmissionsPerUnitHeat
    //             });
    //         }

    //         // Step 3.1: Log a message if demand is not met
    //         if (remainingHeatDemand > 0)
    //         {
    //             Console.WriteLine($"Warning: Heat demand of {heatDemand} could not be fully met. Remaining demand: {remainingHeatDemand}");
    //         }

    //         // Step 4: Add summary data
    //         results.Add(new HeatProductionResult
    //         {
    //             AssetId = "Summary",
    //             HeatProduced = heatDemand - remainingHeatDemand,
    //             ProductionCost = totalProductionCost,
    //             Emissions = totalEmissions
    //         });

    //         return results;
    //     }
    // }

    // // Helper class to store the result of heat production optimization
    // public class HeatProductionResult
    // {
    //     public string AssetId { get; set; }
    //     public double HeatProduced { get; set; }
    //     public double ProductionCost { get; set; }
    //     public double Emissions { get; set; }
    // }
