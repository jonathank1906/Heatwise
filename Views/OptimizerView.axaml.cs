using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

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

              // Assign colors based on machine name
              var bars = assetNames.Select((name, index) =>
              {
                  var color = machineNameColors.ContainsKey(name) ? machineNameColors[name] : Colors.Gray;

                  return new Bar
                  {
                      Position = index + 1,
                      Value = heatProduced[index],
                      FillColor = color,
                  };
              }).ToArray();

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
    private string GetMachineType(string assetName)
    {
        // Example logic to extract machine type from asset name
        if (assetName.Contains("Gas Boiler")) return "Gas Boiler";
        if (assetName.Contains("Oil Boiler")) return "Oil Boiler";
        if (assetName.Contains("Gas Motor")) return "Gas Motor";
        if (assetName.Contains("Heat Pump")) return "Heat Pump";
        return "Unknown";
    }
}