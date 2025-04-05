using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Diagnostics;

using Sem2Proj.ViewModels;

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
                viewModel.PlotOptimizationResults = (assetNames, heatProduced) =>
                {
                    var plt = OptimizationPlot.Plot;
                    plt.Clear();
                    var bgColor = new Color("#1e1e1e");
                    plt.FigureBackground.Color = bgColor;
                    plt.DataBackground.Color = bgColor;
                  
                    var bars = assetNames.Select((name, index) => new Bar
                    {
                        Position = index + 1,
                        Value = heatProduced[index],
                        FillColor = Colors.Red,
                    }).ToArray();
                    Debug.WriteLine("Heat Produced: " + string.Join(", ", heatProduced));
                    // Add bars to the plot
                    plt.Add.Bars(bars);

                    // Add custom ticks for the x-axis
                    var ticks = assetNames.Select((name, index) => new Tick(index + 1, name)).ToArray();
                    plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
                    plt.Axes.Bottom.MajorTickStyle.Length = 0;
                    plt.Axes.Color(new Color("#FFFFFF"));
                    // Style the plot
                    plt.Title("Heat Production Optimization");
                    plt.XLabel("Assets");
                    plt.YLabel("Heat Produced (MW)");
                    plt.Axes.Margins(bottom: 0); // Remove padding below bars
                    plt.HideGrid();

                    OptimizationPlot.Refresh();
                };
            }
        };
    }
}