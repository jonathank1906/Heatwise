using System;
using System.Collections.Generic;
using System.Linq;
using Sem2Proj.Models;

namespace Sem2Proj.Models
{
    public class Optimizer
    {
        private readonly List<AssetModel> _assets;

        public Optimizer(List<AssetModel> assets)
        {
            _assets = assets;
        }

        /// <summary>
        /// Objective 1: Produce heat to achieve a target output at the lowest cost.
        /// A simple greedy algorithm is used here as a starting point.
        /// </summary>
        /// <param name="targetHeat">The required total heat output.</param>
        /// <returns>A list of selected assets.</returns>
        public List<AssetModel> OptimizeHeatProduction(double targetHeat)
        {
            // Sort assets by cost per unit heat (cost/MaxHeat)
            var sortedAssets = _assets.Where(a => a.MaxHeat > 0)
                                      .OrderBy(a => a.ProductionCosts / a.MaxHeat)
                                      .ToList();

            List<AssetModel> selectedAssets = new();
            double accumulatedHeat = 0;

            foreach (var asset in sortedAssets)
            {
                if (accumulatedHeat >= targetHeat)
                    break;

                selectedAssets.Add(asset);
                accumulatedHeat += asset.MaxHeat;
            }

            return selectedAssets;
        }

        /// <summary>
        /// Objective 2: Optimize electricity production.
        /// Separate production from consumption and sort based on a simplistic metric.
        /// </summary>
        /// <returns>A tuple with the list of producers and consumers.</returns>
        public (List<AssetModel> Producers, List<AssetModel> Consumers) OptimizeElectricity()
        {
            // Consider assets with a positive MaxElectricity as producers
            var producers = _assets.Where(a => a.MaxElectricity > 0)
                                   .OrderByDescending(a => a.MaxElectricity)  // Adjust this logic as needed for profit margin
                                   .ToList();

            // Assets with negative MaxElectricity consume electricity. We sort them by lowest absolute consumption.
            var consumers = _assets.Where(a => a.MaxElectricity < 0)
                                   .OrderBy(a => Math.Abs(a.MaxElectricity))
                                   .ToList();

            return (producers, consumers);
        }
    }
}