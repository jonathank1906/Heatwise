using ScottPlot;
using ScottPlot.Avalonia;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot.Plottables;
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

    private Scatter? _heatDemandPlot;

    public void PlotHeatProduction(AvaPlot optimizationPlot, List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData)
    {
        var plt = optimizationPlot.Plot;
        plt.Clear();
        
        // Set dark theme
        var bgColor = new Color("#1e1e1e");
        plt.FigureBackground.Color = bgColor;
        plt.DataBackground.Color = bgColor;
        plt.Axes.Color(new Color("#FFFFFF"));
        plt.Legend.ShadowOffset = new(0, 0);
        plt.Legend.BackgroundColor = new Color("#1e1e1e");
        plt.Legend.OutlineColor = new Color("#1e1e1e");
        plt.Legend.FontColor = Colors.White;

        // Clear previous legend items
        plt.Legend.ManualItems.Clear();

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

                    if (addedToLegend.Add(result.AssetName))
                    {
                        plt.Legend.ManualItems.Add(new LegendItem
                        {
                            LabelText = result.AssetName,
                            FillColor = color
                        });
                    }
                }
            }
        }

        // Create heat demand plot
        var orderedDemand = heatDemandData.OrderBy(x => x.timestamp).ToList();
        double[] positions = new double[orderedDemand.Count + 1];
        double[] values = new double[orderedDemand.Count + 1];

        for (int i = 0; i < orderedDemand.Count; i++)
        {
            positions[i] = i + 0.5;
            values[i] = orderedDemand[i].value;
        }

        positions[^1] = orderedDemand.Count + 0.5;
        values[^1] = values[^2];

        _heatDemandPlot = plt.Add.Scatter(positions, values);
        _heatDemandPlot.ConnectStyle = ConnectStyle.StepHorizontal;
        _heatDemandPlot.LineWidth = 2;
        _heatDemandPlot.Color = Colors.Red.WithAlpha(0.7);
        _heatDemandPlot.MarkerSize = 0;
        _heatDemandPlot.IsVisible = true;

        plt.Title("Heat Production Optimization");
        plt.XLabel("Days");
        plt.YLabel("Heat (MW)");
        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        plt.HideGrid();

        // plt.Legend.IsVisible = true;
        // plt.Legend.Location = Alignment.UpperCenter;
        // plt.Legend.Orientation = Orientation.Horizontal; 
        // plt.Legend.Margin = new(0, 0, 0, -25); 
        // plt.Legend.Alignment = Alignment.UpperCenter;

        optimizationPlot.Refresh();
    }

    public void PlotElectricityPrice(AvaPlot optimizationPlot, List<double> prices)
    {
        var plt = optimizationPlot.Plot;
        plt.Clear();

        // Set dark theme
        var bgColor = new Color("#1e1e1e");
        plt.FigureBackground.Color = bgColor;
        plt.DataBackground.Color = bgColor;
        plt.Axes.Color(new Color("#FFFFFF"));
        plt.Legend.ShadowOffset = new(0, 0);
        plt.Legend.BackgroundColor = new Color("#1e1e1e");
        plt.Legend.OutlineColor = new Color("#1e1e1e");
        plt.Legend.FontColor = Colors.White;

        // Plot electricity prices
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

        plt.Title("Electricity Prices");
        plt.XLabel("Time");
        plt.YLabel("Price");
        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        plt.HideGrid();

        optimizationPlot.Refresh();
    }
    
}