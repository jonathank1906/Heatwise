using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Collections.Generic;
using Sem2Proj.ViewModels;

namespace Sem2Proj.Views;

public partial class OptimizerView : UserControl
{
    private ScottPlot.Plottables.Scatter? _heatDemandPlot;
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

    public OptimizerView()
    {
        InitializeComponent();

        DataContextChanged += (sender, e) =>
        {
            if (DataContext is OptimizerViewModel viewModel)
            {
                viewModel.PlotOptimizationResults = (results, heatDemandData) =>
                {
                    var plt = OptimizationPlot.Plot;
                    plt.Clear();
                    _heatDemandPlot = null;

                    // Set dark theme
                    var bgColor = new Color("#1e1e1e");
                    plt.FigureBackground.Color = bgColor;
                    plt.DataBackground.Color = bgColor;
                    plt.Axes.Color(new Color("#FFFFFF"));

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

                                // Add to legend only once per asset
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

                    // Always create heat demand plot (visibility controlled by checkbox)
                    var orderedDemand = heatDemandData
                        .OrderBy(x => x.timestamp)
                        .ToList();

                    double[] positions = new double[orderedDemand.Count + 1];
                    double[] values = new double[orderedDemand.Count + 1];
                    
                    for (int i = 0; i < orderedDemand.Count; i++)
                    {
                        positions[i] = i + 0.5;
                        values[i] = orderedDemand[i].value;
                    }
                    
                    // Add final point
                    positions[^1] = orderedDemand.Count + 0.5;
                    values[^1] = values[^2];

                    _heatDemandPlot = plt.Add.Scatter(positions, values);
                    _heatDemandPlot.ConnectStyle = ConnectStyle.StepHorizontal;
                    _heatDemandPlot.LineWidth = 2;
                    _heatDemandPlot.Color = Colors.Red.WithAlpha(0.7);
                    _heatDemandPlot.MarkerSize = 0;
                    _heatDemandPlot.IsVisible = viewModel.ShowHeatDemand;

                    // Only add to legend if checkbox is checked
                    if (viewModel.ShowHeatDemand)
                    {
                        plt.Legend.ManualItems.Add(new LegendItem
                        {
                            LabelText = "Heat Demand",
                            LineColor = Colors.Red,
                            LineWidth = 2
                        });
                    }

                    plt.ShowLegend(Alignment.UpperRight);
                    plt.Title("Heat Production Optimization");
                    plt.XLabel("Time Intervals");
                    plt.YLabel("Heat (MW)");
                    plt.Axes.Margins(bottom: 0, top: 0.2);
                    plt.HideGrid();

                    OptimizationPlot.Refresh();
                };

                // Handle toggle changes
                viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(OptimizerViewModel.ShowHeatDemand) && _heatDemandPlot != null)
                    {
                        // Update visibility
                        _heatDemandPlot.IsVisible = viewModel.ShowHeatDemand;
                        
                        // Update legend
                        var plt = OptimizationPlot.Plot;
                        
                        // Remove any existing heat demand legend item
                        var existingItem = plt.Legend.ManualItems.FirstOrDefault(x => x.LabelText == "Heat Demand");
                        if (existingItem != null)
                        {
                            plt.Legend.ManualItems.Remove(existingItem);
                        }
                        
                        // Add to legend if checkbox is checked
                        if (viewModel.ShowHeatDemand)
                        {
                            plt.Legend.ManualItems.Add(new LegendItem
                            {
                                LabelText = "Heat Demand",
                                LineColor = Colors.Red,
                                LineWidth = 2
                            });
                        }
                        
                        OptimizationPlot.Refresh();
                    }
                };
            }
        };
    }
}