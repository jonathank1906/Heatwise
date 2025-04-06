using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Sem2Proj.ViewModels;
using ScottPlot.Colormaps;

namespace Sem2Proj.Views;

public partial class OptimizerView : UserControl
{
    public OptimizerView()
    {
        InitializeComponent();

        // Bind the ViewModel's PlotOptimizationResults action to update the ScottPlot graph
        DataContextChanged += (sender, e) =>
        {
            if (DataContext is OptimizerViewModel viewModel)
            {
                viewModel.PlotOptimizationResults = (results) =>
                {
                    var plt = OptimizationPlot.Plot;
                    plt.Clear();

                    // Set dark theme colors
                    var bgColor = new Color("#1e1e1e");
                    plt.FigureBackground.Color = bgColor;
                    plt.DataBackground.Color = bgColor;
                    plt.Axes.Color(new Color("#FFFFFF"));

                    // Group results by timestamp (each interval will be one stacked bar)
                    var groupedResults = results
                        .Where(r => r.AssetName != "Interval Summary") // Exclude summary entries
                        .GroupBy(r => r.Timestamp)
                        .OrderBy(g => g.Key)
                        .ToList();

                    // Define colors for specific machine names
                    var machineNameColors = new Dictionary<string, Color>
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

                    // Create legend items
                    plt.Legend.ManualItems.Clear();
                    foreach (var kvp in machineNameColors)
                    {
                        plt.Legend.ManualItems.Add(new LegendItem
                        {
                            LabelText = kvp.Key,
                            FillColor = kvp.Value
                        });
                    }
                    plt.ShowLegend(Alignment.UpperRight);

                    // Create stacked bars for each time interval
                    for (int intervalIndex = 0; intervalIndex < groupedResults.Count; intervalIndex++)
                    {
                        var intervalGroup = groupedResults[intervalIndex];
                        double currentBase = 0;

                        foreach (var result in intervalGroup.OrderBy(r => r.AssetName))
                        {
                            var color = machineNameColors.ContainsKey(result.AssetName) 
                                ? machineNameColors[result.AssetName] 
                                : Colors.Gray;

                            var bar = new Bar
                            {
                                Position = intervalIndex + 1,
                                ValueBase = currentBase,
                                Value = currentBase + result.HeatProduced,
                                FillColor = color
                                // Removed the Label property to eliminate in-graph labels
                            };

                            plt.Add.Bar(bar);
                            currentBase += result.HeatProduced;
                        }
                    }

                    // Style the plot
                    plt.Title("Heat Production Optimization");
                    plt.XLabel("Time Intervals");
                    plt.YLabel("Heat Produced (MW)");
                    
                    // Remove all x-axis tick labels to declutter
                    plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                    plt.Axes.Bottom.MajorTickStyle.Length = 0;
                    plt.Axes.Bottom.Label.Text = ""; // Remove x-axis label if desired
                    
                    plt.Axes.Margins(bottom: 0, top: 0.2); // Adjust top margin for legend
                    plt.HideGrid();

                    OptimizationPlot.Refresh();
                };
            }
        };
    }
}