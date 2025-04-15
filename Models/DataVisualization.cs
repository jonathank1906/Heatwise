using ScottPlot;
using ScottPlot.Avalonia;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Sem2Proj.Models;

public class DataVisualization
{
    private readonly Dictionary<string, Color> _machineColors = new()
    {
        { "Gas Boiler 1", Colors.Orange },
        { "Gas Boiler 2", Colors.DarkOrange },
        { "Oil Boiler 1", Colors.Brown },
        { "Oil Boiler 2", Colors.SaddleBrown },
        { "Gas Motor 1", Colors.Blue },
        { "Gas Motor 2", Colors.LightBlue },
        { "Heat Pump 1", Colors.Green },
        { "Heat Pump 2", Colors.LightGreen }
    };

    public void PlotHeatProduction(AvaPlot optimizationPlot, List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Heat Production Optimization", "Days", "Heat (MW)");

        // Process optimization results (stacked bars)
        var groupedResults = results
            .Where(r => r.AssetName != "Interval Summary")
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .ToList();

        // Create stacked bars and add to legend
        var addedToLegend = new HashSet<string>();
        for (int i = 0; i < groupedResults.Count; i++)
        {
            double currentBase = 0;
            foreach (var result in groupedResults[i].OrderBy(r => r.AssetName))
            {
                if (_machineColors.TryGetValue(result.AssetName, out var color))
                {
                    plt.Add.Bar(new Bar
                    {
                        Position = i + 1,
                        ValueBase = currentBase,
                        Value = currentBase + result.HeatProduced,
                        FillColor = color
                    });
                    currentBase += result.HeatProduced;
                }
            }
        }
        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotElectricityPrice(AvaPlot optimizationPlot, List<double> prices)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Electricity Prices", "Time", "Price");

        double[] xs = Enumerable.Range(0, prices.Count).Select(i => (double)i).ToArray();
        var plot = plt.Add.Scatter(xs, prices.ToArray());
        plot.Color = Colors.Green;
        plot.LineWidth = 2;
        plot.MarkerSize = 0;

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Electricity Price",
            LineColor = Colors.Green,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotExpenses(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Production Costs Over Time", "Time", "Cost (DKK)");

        // Group results by timestamp and calculate total production cost per timestamp
        var groupedResults = results
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Timestamp = g.Key,
                TotalCost = g.Sum(r => r.ProductionCost)
            })
            .ToList();

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Production Costs",
            LineColor = Colors.Orange,
            LineWidth = 2
        });

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] costs = groupedResults.Select(g => g.TotalCost).ToArray();

        var costPlot = plt.Add.Scatter(timestamps, costs);
        costPlot.Color = Colors.Orange;
        costPlot.LineWidth = 2;
        costPlot.MarkerSize = 0;

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotEmissions(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Emissions Over Time", "Time", "Emissions (kg CO2)");

        // Group results by timestamp and calculate total emissions per timestamp
        var groupedResults = results
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Timestamp = g.Key,
                TotalEmissions = g.Sum(r => r.Emissions)
            })
            .ToList();

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] emissions = groupedResults.Select(g => g.TotalEmissions).ToArray();

        var emissionsPlot = plt.Add.Scatter(timestamps, emissions);
        emissionsPlot.Color = Colors.Red;
        emissionsPlot.LineWidth = 2;
        emissionsPlot.MarkerSize = 0;

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    private void InitializePlot(Plot plt, string title, string xLabel, string yLabel)
    {
        plt.Clear();
        var bgColor = new Color("#1e1e1e");
        plt.FigureBackground.Color = bgColor;
        plt.DataBackground.Color = bgColor;
        plt.Axes.Color(new Color("#FFFFFF"));
        plt.Legend.ShadowOffset = new(0, 0);
        plt.Legend.BackgroundColor = new Color("#1e1e1e");
        plt.Legend.OutlineColor = new Color("#1e1e1e");
        plt.Legend.FontColor = Colors.White;
        plt.Title(title);
        plt.XLabel(xLabel);
        plt.YLabel(yLabel);
        plt.HideGrid();
    }
}