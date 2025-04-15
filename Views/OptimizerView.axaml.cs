using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Collections.Generic;
using Sem2Proj.ViewModels;
using Avalonia.Interactivity;
using System;
using Sem2Proj.Models;
using Avalonia;
using ScottPlot.Avalonia;

namespace Sem2Proj.Views;

public partial class OptimizerView : UserControl
{
    private AvaPlot _plot;
    private bool _tooltipsEnabled = true;
    private Window? _mainWindow;
    private CalendarWindow? _calendarWindow;
    private TooltipWindow? _tooltipWindow;
    private bool _hasAutoOpenedWindow = false;
    private string? _lastTooltipContent;
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
    private readonly DataVisualization _dataVisualization = new();

    public OptimizerView()
    {
        InitializeComponent();
        _plot = this.Find<AvaPlot>("OptimizationPlot")!;

        DataContextChanged += (sender, e) =>
               {
                   if (DataContext is OptimizerViewModel vm)
                   {
                       vm.PlotOptimizationResults = (results, demand) =>
                           _dataVisualization.PlotHeatProduction(OptimizationPlot, results, demand);
                       vm.PlotElectricityPrices = (prices) =>
                           _dataVisualization.PlotElectricityPrice(OptimizationPlot, prices);
                       vm.PlotExpenses = (results) =>
                           _dataVisualization.PlotExpenses(OptimizationPlot, results);
                   }
               };


        this.AttachedToVisualTree += (s, e) =>
  {
      _mainWindow = TopLevel.GetTopLevel(this) as Window;
      if (_mainWindow != null)
      {
          _mainWindow.PropertyChanged += MainWindow_PropertyChanged;
      }
  };

        this.DetachedFromVisualTree += (s, e) =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.PropertyChanged -= MainWindow_PropertyChanged;
                _mainWindow = null;
            }
        };

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



    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
        {
            if (_mainWindow?.WindowState == WindowState.Minimized)
            {
                _tooltipWindow?.MinimizeWithMainWindow();
                _calendarWindow?.MinimizeWithMainWindow();
            }
            else
            {
                _tooltipWindow?.RestoreWithMainWindow();
                _calendarWindow?.RestoreWithMainWindow();
            }
        }
    }

    private void InitializeTooltipWindow()
    {
        if (_tooltipWindow == null || _tooltipWindow.IsClosed)
        {
            _tooltipWindow = new TooltipWindow();
            _tooltipWindow.Position = new PixelPoint(100, 100); // Default position
            _tooltipWindow.WindowClosed += (s, e) =>
            {
                _tooltipWindow = null;
                _tooltipsEnabled = false;
                if (_hoverCrosshair != null)
                {
                    _hoverCrosshair.IsVisible = false;
                    OptimizationPlot.Refresh();
                }
            };
            _tooltipWindow.Show();
            _tooltipsEnabled = true;
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

        _hoverCrosshair.VerticalLine.Color = Colors.Red.WithAlpha(0.6);
        _hoverCrosshair.HorizontalLine.Color = Colors.Red.WithAlpha(0.6);
        _hoverCrosshair.VerticalLine.LineWidth = 1.5f;
        _hoverCrosshair.HorizontalLine.LineWidth = 1.5f;

        // Set x-axis ticks to show only dates (no time)
        string[] labels = new string[groupedResults.Count];
        double[] tickPositions = new double[groupedResults.Count];

        // Track the current day to only show label when day changes
        DateTime currentDay = DateTime.MinValue;
        for (int i = 0; i < groupedResults.Count; i++)
        {
            var timestamp = groupedResults[i].Key;
            if (timestamp.Date != currentDay)
            {
                labels[i] = timestamp.ToString("MM/dd");
                currentDay = timestamp.Date;
            }
            else
            {
                labels[i] = string.Empty;
            }
            tickPositions[i] = i + 1;
        }

        plt.Axes.Bottom.SetTicks(tickPositions, labels);
        plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
        plt.Axes.Bottom.TickLabelStyle.OffsetY = 20;

        OptimizationPlot.PointerMoved += (s, e) =>
        {
            if (_currentFilteredResults == null || _currentHeatDemandData == null || !_tooltipsEnabled)
            {
                _hoverCrosshair.IsVisible = false;
                OptimizationPlot.Refresh();
                return;
            }

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

                _lastTooltipContent = tooltip;

                if (!_hasAutoOpenedWindow && (_tooltipWindow == null || _tooltipWindow.IsClosed))
                {
                    ShowTooltipWindow();
                    _hasAutoOpenedWindow = true;
                }

                UpdateTooltipContent(tooltip);
            }
            else
            {
                _hoverCrosshair.IsVisible = false;
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

        plt.Title("Heat Production Optimization");
        plt.XLabel("Days");
        plt.YLabel("Heat (MW)");
        plt.Axes.Margins(bottom: 0.02, top: 0.1);
        plt.HideGrid();

        OptimizationPlot.Refresh();
    }

    private void ShowTooltipWindow()
    {
        InitializeTooltipWindow();
    }
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _tooltipWindow?.Close();
        _tooltipWindow = null;
        _calendarWindow?.Close();
        _calendarWindow = null;
        _hasAutoOpenedWindow = false;
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

    private void UpdateTooltipContent(string text)
    {
        if (_tooltipWindow == null || _tooltipWindow.IsClosed)
        {
            return;
        }
        _tooltipWindow?.UpdateContent(text);
    }

    private void ToggleTooltip_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as OptimizerViewModel;
        if (viewModel == null || !viewModel.HasOptimized)
        {
            return;
        }

        if (_tooltipWindow == null || _tooltipWindow.IsClosed)
        {
            ShowTooltipWindow();
            _tooltipsEnabled = true;
        }
        else
        {
            _tooltipWindow.Close();
            _tooltipsEnabled = false;
            if (_hoverCrosshair != null)
            {
                _hoverCrosshair.IsVisible = false;
                OptimizationPlot.Refresh();
            }
        }
    }

    private void OpenCalendarPopup(object sender, RoutedEventArgs e)
    {
        if (_currentHeatDemandData == null || !_currentHeatDemandData.Any())
            return;

        if (_calendarWindow == null || _calendarWindow.IsClosed)
        {
            _calendarWindow = new CalendarWindow();
            _calendarWindow.DatesSelected += (s, args) =>
            {
                SetRangeFromCalendar(_calendarWindow.OptimizationCalendar.SelectedDates);
            };

            // Position near the button
            var button = sender as Control;
            var screenPosition = button?.PointToScreen(new Point(0, button.Bounds.Height));
            _calendarWindow.Position = new PixelPoint((int)screenPosition!.Value.X, (int)screenPosition.Value.Y);

            _calendarWindow.InitializeCalendar(_currentHeatDemandData.Select(x => x.timestamp));
            _calendarWindow.Show();
        }
        else
        {
            _calendarWindow.Activate();
        }
    }

    private void SetRangeFromCalendar(IEnumerable<DateTime> selectedDates)
    {
        if (_currentOptimizationResults == null || _currentHeatDemandData == null)
            return;

        var dates = selectedDates.OrderBy(d => d).ToList();
        if (dates.Count == 0) return;

        DateTime startDate = dates.First();
        DateTime endDate = dates.Last().AddDays(1);

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
    private async void OnExportButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is OptimizerViewModel viewModel)
        {
            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                await viewModel.ExportToCsv(parentWindow);
            }
        }
    }
}