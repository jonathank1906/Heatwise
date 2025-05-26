using ScottPlot;
using ScottPlot.Avalonia;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using Avalonia;

namespace Heatwise.Models;

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
            .GroupBy(machine => new { machine.PresetId, machine.Name }) // Group by both ID and Name for uniqueness
            .Select(group => group.First()) // Take the first machine in each group
            .ToList();

        // Populate the dictionary with machine names (including ID for uniqueness) and their colors
        foreach (var machine in allMachines)
        {
            try
            {
                // Convert the color string from the database to a ScottPlot.Color object
                var color = System.Drawing.ColorTranslator.FromHtml(machine.Color);
                var uniqueKey = $"{machine.Name} (ID: {machine.PresetId})";
                _machineColors[uniqueKey] = new ScottPlot.Color(color.R, color.G, color.B, color.A);
                Debug.WriteLine($"[InitializeMachineColors] Machine: {uniqueKey}, Color: {machine.Color}");
            }
            catch
            {
                // Fallback to a default color if parsing fails
                var uniqueKey = $"{machine.Name} (ID: {machine.PresetId})";
                _machineColors[uniqueKey] = ScottPlot.Colors.LightCyan;
                Debug.WriteLine($"[InitializeMachineColors] Failed to parse color for machine {uniqueKey}. Using fallback color.");
            }
        }
    }

    public ScottPlot.Color GetCurrentThemeAxesColor()
    {
        if (Application.Current == null)
        {
            Debug.WriteLine("Application.Current is null");
            return new ScottPlot.Color(255, 255, 255); // Fallback to white
        }
        try
        {
            // Get the current text color from resources
            if (Application.Current.Styles.TryGetResource("TextColor", Application.Current.ActualThemeVariant, out var colorValue)
                && colorValue is Avalonia.Media.Color avaloniaColor)
            {
                Debug.WriteLine($"[GetCurrentThemeTextColor] Retrieved color: R={avaloniaColor.R}, G={avaloniaColor.G}, B={avaloniaColor.B}, A={avaloniaColor.A}");
                return new ScottPlot.Color(
                    avaloniaColor.R,
                    avaloniaColor.G,
                    avaloniaColor.B,
                    avaloniaColor.A);
            }
        }
        catch
        {
            // Fallback color if something goes wrong
            Debug.WriteLine("Couldn't get TextColor from resources");
        }

        return new ScottPlot.Color(255, 255, 255); // Fallback to white
    }

    public ScottPlot.Color GetCurrentThemeBackgroundColor()
{
    if (Application.Current == null)
    {
        Debug.WriteLine("Application.Current is null");
        return new ScottPlot.Color(0, 0, 0); // Fallback to black
    }
    try
    {
        // Get the current background color from resources
        if (Application.Current.Styles.TryGetResource("BackgroundColor", Application.Current.ActualThemeVariant, out var colorValue)
            && colorValue is Avalonia.Media.Color avaloniaColor)
        {
            Debug.WriteLine($"[GetCurrentThemeBackgroundColor] Retrieved color: R={avaloniaColor.R}, G={avaloniaColor.G}, B={avaloniaColor.B}, A={avaloniaColor.A}");
            return new ScottPlot.Color(
                avaloniaColor.R,
                avaloniaColor.G,
                avaloniaColor.B,
                avaloniaColor.A);
        }
    }
    catch
    {
        // Fallback color if something goes wrong
        Debug.WriteLine("Couldn't get BackgroundColor from resources");
    }

    return new ScottPlot.Color(0, 0, 0); // Fallback to black
}

    public void PlotHeatProduction(AvaPlot optimizationPlot, List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData)
    {
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
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
            // Separate unmet demand from regular assets
            var unmetDemand = groupedResults[i].FirstOrDefault(r => r.AssetName == "Unmet Demand");
            var regularAssets = groupedResults[i].Where(r => r.AssetName != "Unmet Demand").ToList();

            // Order regular assets by heat produced (descending) so largest contributions are at bottom
            var orderedAssets = regularAssets
                .OrderByDescending(r => r.HeatProduced)
                .ToList();

            double currentBase = 0;

            // Process regular assets from largest to smallest
            foreach (var result in orderedAssets)
            {
                var possibleKey = $"{result.AssetName} (ID: {result.PresetId})";
                if (_machineColors.TryGetValue(possibleKey, out var color))
                {
                    plt.Add.Bar(new Bar
                    {
                        Position = i,
                        ValueBase = currentBase,
                        Value = currentBase + result.HeatProduced,
                        FillColor = color
                    });

                    currentBase += result.HeatProduced;

                    // Add to legend if not already added
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

            // Add unmet demand on very top if it exists
            if (unmetDemand != null)
            {
                var unmetColor = new ScottPlot.Color(255, 0, 0, 180); // Semi-transparent red
                plt.Add.Bar(new Bar
                {
                    Position = i,
                    ValueBase = currentBase,
                    Value = currentBase + unmetDemand.HeatProduced,
                    FillColor = unmetColor
                });

                if (addedToLegend.Add("Unmet Demand"))
                {
                    plt.Legend.ManualItems.Add(new LegendItem
                    {
                        LabelText = "Unmet Demand",
                        FillColor = unmetColor
                    });
                }
            }
        }

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
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Electricity Prices", "", "Electricity Price (DKK/MWh)");

        double[] xs = Enumerable.Range(0, prices.Count).Select(i => (double)i).ToArray();
        var plot = plt.Add.Scatter(xs, prices.ToArray());
        plot.Color = Colors.Green;
        plot.LineWidth = 2;
        plot.MarkerSize = 5;

        // Add max line
        double max = prices.Max();
        var maxLine = plt.Add.HorizontalLine(max);
        maxLine.Color = ScottPlot.Color.FromHex("006400");
        maxLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add median line
        double median = prices.OrderBy(p => p).ElementAt(prices.Count / 2);
        var medianLine = plt.Add.HorizontalLine(median);
        medianLine.Color = ScottPlot.Color.FromHex("00800040");
        medianLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add min line
        double min = prices.Min();
        var minLine = plt.Add.HorizontalLine(min);
        minLine.Color = ScottPlot.Color.FromHex("006400");
        minLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add legend items for max, median, and min
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Max",
            LineColor = ScottPlot.Color.FromHex("006400"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Median",
            LineColor = ScottPlot.Color.FromHex("00800040"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Min",
            LineColor = ScottPlot.Color.FromHex("006400"),
            LineWidth = 2
        });

        // Add legend item for electricity price
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

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] costs = groupedResults.Select(g => g.TotalCost).ToArray();

        // Plot the production costs
        var costPlot = plt.Add.Scatter(timestamps, costs);
        costPlot.Color = Colors.Orange;
        costPlot.LineWidth = 2;
        costPlot.MarkerSize = 5;

        // Add max line
        double max = costs.Max();
        var maxLine = plt.Add.HorizontalLine(max);
        maxLine.Color = ScottPlot.Color.FromHex("006400");
        maxLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add median line
        double median = costs.OrderBy(c => c).ElementAt(costs.Length / 2);
        var medianLine = plt.Add.HorizontalLine(median);
        medianLine.Color = ScottPlot.Color.FromHex("00800040");
        medianLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add min line
        double min = costs.Min();
        var minLine = plt.Add.HorizontalLine(min);
        minLine.Color = ScottPlot.Color.FromHex("8B0000");
        minLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add legend items for max, median, and min
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Max",
            LineColor = ScottPlot.Color.FromHex("006400"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Median",
            LineColor = ScottPlot.Color.FromHex("00800040"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Min",
            LineColor = ScottPlot.Color.FromHex("8B0000"),
            LineWidth = 2
        });

        // Add legend item for production costs
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Production Costs",
            LineColor = Colors.Orange,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotEmissions(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
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

        // Plot the emissions
        var emissionsPlot = plt.Add.Scatter(timestamps, emissions);
        emissionsPlot.Color = Colors.Red;
        emissionsPlot.LineWidth = 2;
        emissionsPlot.MarkerSize = 5;

        // Add max line
        double max = emissions.Max();
        var maxLine = plt.Add.HorizontalLine(max);
        maxLine.Color = ScottPlot.Color.FromHex("006400");
        maxLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add median line
        double median = emissions.OrderBy(e => e).ElementAt(emissions.Length / 2);
        var medianLine = plt.Add.HorizontalLine(median);
        medianLine.Color = ScottPlot.Color.FromHex("00800040");
        medianLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add min line
        double min = emissions.Min();
        var minLine = plt.Add.HorizontalLine(min);
        minLine.Color = ScottPlot.Color.FromHex("8B0000");
        minLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add legend items for max, median, and min
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Max",
            LineColor = ScottPlot.Color.FromHex("006400"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Median",
            LineColor = ScottPlot.Color.FromHex("00800040"),
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Min",
            LineColor = ScottPlot.Color.FromHex("8B0000"),
            LineWidth = 2
        });

        // Add legend item for emissions
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Emissions",
            LineColor = Colors.Red,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotElectricityConsumption(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Electricity Consumption", "", "Electricity (MWh)");

        // Group results by timestamp and calculate total electricity consumption per timestamp
        var groupedResults = results
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Timestamp = g.Key,
                TotalConsumption = g.Sum(r => r.ElectricityConsumption)
            })
            .ToList();

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] consumption = groupedResults.Select(g => g.TotalConsumption).ToArray();

        // Plot the electricity consumption
        var consumptionPlot = plt.Add.Scatter(timestamps, consumption);
        consumptionPlot.Color = Colors.Blue;
        consumptionPlot.LineWidth = 2;
        consumptionPlot.MarkerSize = 5;

        // Add max line
        double max = consumption.Max();
        var maxLine = plt.Add.HorizontalLine(max);
        maxLine.Color = ScottPlot.Color.FromHex("006400");
        maxLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add legend items
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Electricity Consumption",
            LineColor = Colors.Blue,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotElectricityProduction(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Electricity Production", "", "Electricity (MWh)");

        // Group results by timestamp and calculate total electricity production per timestamp
        var groupedResults = results
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Timestamp = g.Key,
                TotalProduction = g.Sum(r => r.ElectricityProduction)
            })
            .ToList();

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] production = groupedResults.Select(g => g.TotalProduction).ToArray();

        // Plot the electricity production
        var productionPlot = plt.Add.Scatter(timestamps, production);
        productionPlot.Color = Colors.Purple;
        productionPlot.LineWidth = 2;
        productionPlot.MarkerSize = 5;

        // Add max line
        double max = production.Max();
        var maxLine = plt.Add.HorizontalLine(max);
        maxLine.Color = ScottPlot.Color.FromHex("800080"); // Purple
        maxLine.LinePattern = ScottPlot.LinePattern.Dashed;

        // Add legend items
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Electricity Production",
            LineColor = Colors.Purple,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    public void PlotFuelConsumption(AvaPlot optimizationPlot, List<HeatProductionResult> results)
    {
        optimizationPlot.UserInputProcessor.DoubleLeftClickBenchmark(false);
        var plt = optimizationPlot.Plot;
        InitializePlot(plt, "Fuel Consumption", "", "Fuel Consumption (MWh)");

        // Group results by timestamp and calculate total oil and gas consumption per timestamp
        var groupedResults = results
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Timestamp = g.Key,
                TotalOilConsumption = g.Sum(r => r.OilConsumption),
                TotalGasConsumption = g.Sum(r => r.GasConsumption)
            })
            .ToList();

        double[] timestamps = groupedResults.Select((g, i) => (double)i).ToArray();
        double[] oilConsumption = groupedResults.Select(g => g.TotalOilConsumption).ToArray();
        double[] gasConsumption = groupedResults.Select(g => g.TotalGasConsumption).ToArray();

        // Plot oil consumption
        var oilPlot = plt.Add.Scatter(timestamps, oilConsumption);
        oilPlot.Color = Colors.Orange;
        oilPlot.LineWidth = 2;
        oilPlot.MarkerSize = 5;
        oilPlot.Label = "Oil Consumption";

        // Plot gas consumption
        var gasPlot = plt.Add.Scatter(timestamps, gasConsumption);
        gasPlot.Color = Colors.Blue;
        gasPlot.LineWidth = 2;
        gasPlot.MarkerSize = 5;
        gasPlot.Label = "Gas Consumption";

        // Add legend items
        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Oil Consumption",
            LineColor = Colors.Orange,
            LineWidth = 2
        });

        plt.Legend.ManualItems.Add(new LegendItem
        {
            LabelText = "Gas Consumption",
            LineColor = Colors.Blue,
            LineWidth = 2
        });

        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        optimizationPlot.Refresh();
    }

    private void InitializePlot(Plot plt, string title, string xLabel, string yLabel)
    {
        plt.Clear();
        plt.Legend.ManualItems.Clear();
        var bgColor = new Color("{DynamicResource BackgroundColor}");
        plt.FigureBackground.Color = bgColor;
        plt.DataBackground.Color = bgColor;
        plt.Axes.Color(GetCurrentThemeAxesColor());
        plt.Legend.BackgroundColor = GetCurrentThemeBackgroundColor();
        plt.Legend.OutlineColor = GetCurrentThemeAxesColor();
        plt.Legend.FontColor = GetCurrentThemeAxesColor();
        plt.Legend.ShadowOffset = new(0, 0);
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