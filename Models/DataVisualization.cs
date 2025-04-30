using ScottPlot;
using ScottPlot.Avalonia;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

namespace Sem2Proj.Models;

public class DataVisualization
{
    private readonly AssetManager _assetManager;
    private Dictionary<string, Color> _machineColors;
    public DataVisualization(AssetManager assetManager)
    {
        _assetManager = assetManager;
        InitializeMachineColors();
    }
    private void InitializeMachineColors()
    {
        _machineColors = new Dictionary<string, ScottPlot.Color>();

        // Get all machines from all presets
        var allMachines = _assetManager.Presets
            .SelectMany(preset => preset.MachineModels)
            .GroupBy(machine => new { machine.Id, machine.Name }) // Group by both ID and Name for uniqueness
            .Select(group => group.First()) // Take the first machine in each group
            .ToList();

        // Populate the dictionary with machine names (including ID for uniqueness) and their colors
        foreach (var machine in allMachines)
        {
            try
            {
                // Convert the color string from the database to a ScottPlot.Color object
                var color = System.Drawing.ColorTranslator.FromHtml(machine.Color);
                var uniqueKey = $"{machine.Name} (ID: {machine.Id})";
                _machineColors[uniqueKey] = new ScottPlot.Color(color.R, color.G, color.B, color.A);
                Debug.WriteLine($"[InitializeMachineColors] Machine: {uniqueKey}, Color: {machine.Color}");
            }
            catch
            {
                // Fallback to a default color if parsing fails
                var uniqueKey = $"{machine.Name} (ID: {machine.Id})";
                _machineColors[uniqueKey] = ScottPlot.Colors.LightCyan;
                Debug.WriteLine($"[InitializeMachineColors] Failed to parse color for machine {uniqueKey}. Using fallback color.");
            }
        }
    }

    public void RefreshMachineColors()
    {
        InitializeMachineColors();
    }
    public void PlotHeatProduction(AvaPlot optimizationPlot, List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Heat Production Optimization", "", "Heat (MW)");

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
                var possibleKey = $"{result.AssetName} (ID: {result.PresetId})";
                Debug.WriteLine($"[PlotHeatProduction] Checking for key: {possibleKey}");

                if (_machineColors.TryGetValue(possibleKey, out var color))
                {
                    Debug.WriteLine($"[PlotHeatProduction] Found color for key: {possibleKey}, Color: {color}");

                    plt.Add.Bar(new Bar
                    {
                        Position = i,
                        ValueBase = currentBase,
                        Value = currentBase + result.HeatProduced,
                        FillColor = color
                    });
                    Debug.WriteLine($"[PlotHeatProduction] Added bar for {result.AssetName} at position {i} with value {result.HeatProduced}");

                    currentBase += result.HeatProduced;

                    // Add to legend if not already added
                    if (addedToLegend.Add(result.AssetName))
                    {
                        plt.Legend.ManualItems.Add(new LegendItem
                        {
                            LabelText = result.AssetName,
                            FillColor = color
                        });
                        Debug.WriteLine($"[PlotHeatProduction] Added legend item for {result.AssetName}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[PlotHeatProduction] No color found for key: {possibleKey}");
                }
            }
        }

        // Add heat demand line to the plot
        if (heatDemandData != null && heatDemandData.Any())
        {
            var orderedDemand = heatDemandData
            .OrderBy(x => x.timestamp)
            .ToList();

            double[] positions = new double[orderedDemand.Count + 1];
            double[] values = new double[orderedDemand.Count + 1];

            for (int i = 0; i < orderedDemand.Count; i++)
            {
                positions[i] = i - 0.5;
                values[i] = orderedDemand[i].value;
            }

            positions[^1] = orderedDemand.Count - 0.5;
            values[^1] = values[^2];

            var heatDemandLine = plt.Add.Scatter(positions, values);
            heatDemandLine.Color = Colors.Red;
            heatDemandLine.LineWidth = 2;
            heatDemandLine.MarkerSize = 0;
            heatDemandLine.ConnectStyle = ConnectStyle.StepHorizontal;

            plt.Legend.ManualItems.Add(new LegendItem
            {
                LabelText = "Heat Demand",
                LineColor = Colors.Red,
                LineWidth = 2
            });
        }
        SetXAxisTicks(plt, groupedResults.Select(g => g.Key).ToList());
        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotElectricityPrice(AvaPlot optimizationPlot, List<double> prices)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Electricity Prices", "", "Electricity Price (DKK/MWh)");

        double[] xs = Enumerable.Range(0, prices.Count).Select(i => (double)i).ToArray();
        var plot = plt.Add.Scatter(xs, prices.ToArray());
        plot.Color = Colors.Green;
        plot.LineWidth = 2;
        plot.MarkerSize = 5;

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
        InitializePlot(plt, "Production Costs", "", "Cost (DKK)");

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
        costPlot.MarkerSize = 5;

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotEmissions(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Emissions", "", "Emissions (kg CO2)");

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
        emissionsPlot.MarkerSize = 5;

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    private void InitializePlot(Plot plt, string title, string xLabel, string yLabel)
    {
        plt.Clear();

        plt.Legend.ManualItems.Clear();
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

    public void SetXAxisTicks(Plot plt, List<DateTime> timestamps)
    {
        string[] labels = new string[timestamps.Count];
        double[] tickPositions = new double[timestamps.Count];

        DateTime currentDay = DateTime.MinValue;
        for (int i = 0; i < timestamps.Count; i++)
        {
            var timestamp = timestamps[i];
            if (timestamp.Date != currentDay)
            {
                labels[i] = timestamp.ToString("MM/dd");
                currentDay = timestamp.Date;
            }
            else
            {
                labels[i] = string.Empty;
            }
            tickPositions[i] = i;
        }

        plt.Axes.Bottom.SetTicks(tickPositions, labels);
    }
}