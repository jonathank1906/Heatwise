using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Collections.Generic;
using Sem2Proj.ViewModels;
using Avalonia.Interactivity;
using System;
using Avalonia.Controls.Primitives;
using Sem2Proj.Models;
using Avalonia.Input;
using ScottPlot;
using Avalonia;
using ScottPlot;
using Avalonia.Input;

namespace Sem2Proj.Views;

public partial class OptimizerView : UserControl
{
    private ScottPlot.Plottables.Scatter? _heatDemandPlot;
    private ScottPlot.Plottables.Crosshair? _hoverCrosshair;
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

    private List<(DateTime timestamp, double value)>? _currentHeatDemandData;
    private List<HeatProductionResult>? _currentOptimizationResults;
    private List<HeatProductionResult>? _currentFilteredResults;

    public OptimizerView()
    {
        InitializeComponent();
        
        DataContextChanged += (sender, e) =>
        {
            if (DataContext is OptimizerViewModel viewModel)
            {
                viewModel.PlotOptimizationResults = (results, heatDemandData) =>
                {
                    _currentOptimizationResults = results;
                    _currentHeatDemandData = heatDemandData;
                    _currentFilteredResults = results;
                    PlotResults(results, heatDemandData, viewModel.ShowHeatDemand);
                    InitializeCalendar(heatDemandData);
                };

                viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(OptimizerViewModel.ShowHeatDemand) && _heatDemandPlot != null)
                    {
                        UpdateHeatDemandVisibility(viewModel.ShowHeatDemand);
                    }
                };
            }
        };
    }

    private void InitializeCalendar(List<(DateTime timestamp, double value)> heatDemandData)
    {
        if (heatDemandData == null || !heatDemandData.Any()) 
            return;

        var dates = heatDemandData.Select(x => x.timestamp.Date).Distinct().ToList();
        if (!dates.Any()) return;

        OptimizationCalendar.DisplayDateStart = dates.Min();
        OptimizationCalendar.DisplayDateEnd = dates.Max();
        OptimizationCalendar.DisplayDate = dates.First();
        
        // Blackout dates without data
        OptimizationCalendar.BlackoutDates.Clear();
        
        var allDates = new List<DateTime>();
        for (var date = dates.Min(); date <= dates.Max(); date = date.AddDays(1))
        {
            allDates.Add(date);
        }

        var datesWithoutData = allDates.Except(dates).ToList();
        if (datesWithoutData.Count == 0) return;

        DateTime? rangeStart = null;
        DateTime? rangeEnd = null;

        foreach (var date in datesWithoutData.OrderBy(d => d))
        {
            if (!rangeStart.HasValue)
            {
                rangeStart = date;
                rangeEnd = date;
            }
            else if (date == rangeEnd.Value.AddDays(1))
            {
                rangeEnd = date;
            }
            else
            {
                OptimizationCalendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
                rangeStart = date;
                rangeEnd = date;
            }
        }

        if (rangeStart.HasValue)
        {
            OptimizationCalendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
        }
    }

    private void PlotResults(List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData, bool showHeatDemand)
    {
        var plt = OptimizationPlot.Plot;
        plt.Clear();
        _heatDemandPlot = null;
        _hoverCrosshair = null;
        _currentFilteredResults = results;

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

        // Create heat demand plot with proper offset
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
        
        positions[^1] = orderedDemand.Count + 0.5;
        values[^1] = values[^2];

        _heatDemandPlot = plt.Add.Scatter(positions, values);
        _heatDemandPlot.ConnectStyle = ConnectStyle.StepHorizontal;
        _heatDemandPlot.LineWidth = 2;
        _heatDemandPlot.Color = Colors.Red.WithAlpha(0.7);
        _heatDemandPlot.MarkerSize = 0;
        _heatDemandPlot.IsVisible = showHeatDemand;

        // Add hover crosshair
        _hoverCrosshair = plt.Add.Crosshair(0, 0);
        _hoverCrosshair.IsVisible = false;
       // _hoverCrosshair.VerticalLine.Pattern = LinePattern.DenselyDashed;
       // _hoverCrosshair.HorizontalLine.Pattern = LinePattern.DenselyDashed;

        // Set x-axis ticks to show date and time
        string[] labels = new string[groupedResults.Count];
        double[] tickPositions = new double[groupedResults.Count];
        for (int i = 0; i < groupedResults.Count; i++)
        {
            labels[i] = groupedResults[i].Key.ToString("MM/dd HH:mm");
            tickPositions[i] = i + 1;
        }
        
        if (groupedResults.Count > 20)
        {
            int step = (int)Math.Ceiling(groupedResults.Count / 20.0);
            for (int i = 0; i < groupedResults.Count; i++)
            {
                if (i % step != 0) labels[i] = string.Empty;
            }
        }

        plt.Axes.Bottom.SetTicks(tickPositions, labels);
        plt.Axes.Bottom.TickLabelStyle.Rotation = 45;

        // Add hover interaction
    OptimizationPlot.PointerMoved += (s, e) => 
    {
        if (_currentFilteredResults == null || _currentHeatDemandData == null)
            return;

        var pixelPosition = e.GetPosition(OptimizationPlot);
        var pixel = new Pixel((float)pixelPosition.X, (float)pixelPosition.Y);
        var coordinates = plt.GetCoordinates(pixel);
        
        int barIndex = (int)Math.Round(coordinates.X - 0.5);
        
        if (barIndex >= 0 && barIndex < groupedResults.Count)
        {
            var timestamp = groupedResults[barIndex].Key;
            var resultsAtTime = _currentFilteredResults
                .Where(r => r.Timestamp == timestamp && r.AssetName != "Interval Summary")
                .ToList();
            
            var heatDemand = _currentHeatDemandData
                .FirstOrDefault(h => h.timestamp == timestamp).value;

            string tooltip = $"Time: {timestamp:MM/dd HH:mm}\n";
            tooltip += $"Heat Demand: {heatDemand:F2} MW\n";
            tooltip += "Production:\n";
            
            foreach (var result in resultsAtTime)
            {
                tooltip += $"- {result.AssetName}: {result.HeatProduced:F2} MW\n";
            }

            _hoverCrosshair.IsVisible = true;
            _hoverCrosshair.VerticalLine.Position = barIndex + 1;
            _hoverCrosshair.HorizontalLine.Position = heatDemand;
            
            plt.Title(tooltip);
         //  plt.Title.Label.FontSize = 12;
          //  plt.Title.Label.FontColor = Colors.White;
        }
        else
        {
            _hoverCrosshair.IsVisible = false;
          //  plt.Title.Label.Text = originalTitle;
        }

        OptimizationPlot.Refresh();
    };

        if (showHeatDemand)
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
        plt.Axes.Margins(bottom: 0.1, top: 0.3);
        plt.HideGrid();

        OptimizationPlot.Refresh();
    }

    private void UpdateHeatDemandVisibility(bool showHeatDemand)
    {
        if (_heatDemandPlot == null) return;
        
        var plt = OptimizationPlot.Plot;
        _heatDemandPlot.IsVisible = showHeatDemand;
        
        var existingItem = plt.Legend.ManualItems.FirstOrDefault(x => x.LabelText == "Heat Demand");
        if (existingItem != null)
        {
            plt.Legend.ManualItems.Remove(existingItem);
        }
        
        if (showHeatDemand)
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

    private void SetRange_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOptimizationResults == null || _currentHeatDemandData == null) 
            return;

        if (OptimizationCalendar.SelectedDates.Count == 0) 
            return;

        var selectedDates = OptimizationCalendar.SelectedDates.OrderBy(d => d).ToList();
        DateTime startDate = selectedDates.First();
        DateTime endDate = selectedDates.Last().AddDays(1);

        var filteredResults = _currentOptimizationResults
            .Where(r => r.Timestamp.Date >= startDate && r.Timestamp.Date <= endDate)
            .ToList();

        var filteredHeatDemand = _currentHeatDemandData
            .Where(h => h.timestamp.Date >= startDate && h.timestamp.Date <= endDate)
            .ToList();

        if (!filteredResults.Any() || !filteredHeatDemand.Any())
        {
            return;
        }

        _currentFilteredResults = filteredResults;
        PlotResults(filteredResults, filteredHeatDemand, (DataContext as OptimizerViewModel)?.ShowHeatDemand ?? true);
    }

    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOptimizationResults == null || _currentHeatDemandData == null) 
            return;

        OptimizationCalendar.SelectedDates.Clear();
        _currentFilteredResults = _currentOptimizationResults;
        PlotResults(_currentOptimizationResults, _currentHeatDemandData, (DataContext as OptimizerViewModel)?.ShowHeatDemand ?? true);
    }
}