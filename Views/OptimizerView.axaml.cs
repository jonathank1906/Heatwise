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
                {
                    _currentOptimizationResults = results;

                    _currentFilteredResults = results;
                    // Call the existing plot method
                    _dataVisualization.PlotHeatProduction(OptimizationPlot, results, demand);

                    // Add the crosshair functionality
                    PlotCrosshair(results, demand);

                };

                vm.PlotElectricityPrices = (prices) =>
                {
                    var priceValues = prices.Select(p => p.price).ToList();  // Extract the price values

                    _dataVisualization.PlotElectricityPrice(OptimizationPlot, priceValues);

                    // Convert prices to a format compatible with PlotCrosshair
                    var dummyResults = prices.Select(p => new HeatProductionResult
                    {
                        Timestamp = p.timestamp,
                    }).ToList();

                    var dummyDemand = prices.Select(p => (p.timestamp, p.price)).ToList();
                    PlotCrosshair(dummyResults, dummyDemand);
                };

                vm.PlotExpenses = (results) =>
                 {
                     // Call the existing plot method
                     _dataVisualization.PlotExpenses(OptimizationPlot, results);

                     // Add the crosshair functionality
                     var dummyDemand = new List<(DateTime timestamp, double value)>(); // Empty demand for compatibility
                     PlotCrosshair(results, dummyDemand);
                 };

                vm.PlotEmissions = (results) =>
                {
                    // Call the existing plot method
                    _dataVisualization.PlotEmissions(OptimizationPlot, results);

                    // Add the crosshair functionality
                    var dummyDemand = new List<(DateTime timestamp, double value)>(); // Empty demand for compatibility
                    PlotCrosshair(results, dummyDemand);
                };
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
    }

    private void PlotCrosshair(List<HeatProductionResult> results, List<(DateTime timestamp, double value)> heatDemandData)
    {
        var plt = OptimizationPlot.Plot;

        _hoverCrosshair = null;
        _currentFilteredResults = results;
        _currentHeatDemandData = heatDemandData;

        _hoverCrosshair = plt.Add.Crosshair(0, 0);
        _hoverCrosshair.IsVisible = false;

        _hoverCrosshair.VerticalLine.Color = Colors.Red.WithAlpha(0.6);
        _hoverCrosshair.HorizontalLine.Color = Colors.Red.WithAlpha(0.6);
        _hoverCrosshair.VerticalLine.LineWidth = 1.5f;
        _hoverCrosshair.HorizontalLine.LineWidth = 1.5f;

        // Group results by timestamp
        var groupedResults = results
            .Where(r => r.AssetName != "Interval Summary")
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .ToList();

        // Crosshair interaction
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

                string tooltip = $"Time: {timestamp:MM/dd HH:mm}\n";

                switch ((DataContext as OptimizerViewModel)?.SelectedGraphType)
                {
                    case OptimizerViewModel.GraphType.HeatProduction:
                        var resultsAtTime = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp && r.AssetName != "Interval Summary")
                            .ToList();

                        var heatDemand = _currentHeatDemandData
                            .FirstOrDefault(h => h.timestamp == timestamp).value;

                        tooltip += $"Heat Demand: {heatDemand:F2} MW\n";
                        tooltip += "Production:\n";

                        foreach (var result in resultsAtTime)
                        {
                            tooltip += $"- {result.AssetName}: {result.HeatProduced:F2} MW\n";
                        }

                        _hoverCrosshair.HorizontalLine.Position = heatDemand;
                        break;

                    case OptimizerViewModel.GraphType.ElectricityPrices:
                        var electricityPrice = _currentHeatDemandData
                            .FirstOrDefault(h => h.timestamp == timestamp).value;

                        tooltip += $"Electricity Price: {electricityPrice:F2} DKK\n";
                        _hoverCrosshair.HorizontalLine.Position = electricityPrice;
                        break;

                    case OptimizerViewModel.GraphType.ProductionCosts:
                        var productionCost = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.ProductionCost);

                        tooltip += $"Production Cost: {productionCost:F2} DKK\n";
                        _hoverCrosshair.HorizontalLine.Position = productionCost;
                        break;

                    case OptimizerViewModel.GraphType.CO2Emissions:
                        var emissions = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.Emissions);

                        tooltip += $"CO2 Emissions: {emissions:F2} kg\n";
                        _hoverCrosshair.HorizontalLine.Position = emissions;
                        break;

                    default:
                        tooltip += "No data available for this graph type.\n";
                        break;
                }

                _hoverCrosshair.IsVisible = true;
                _hoverCrosshair.VerticalLine.Position = barIndex + 1;

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
            _tooltipWindow.Position = new PixelPoint(100, 100);
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

    // Calendar
    // -------------------------------------------------
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
            _calendarWindow.Position = new PixelPoint((int)screenPosition.Value.X, (int)screenPosition.Value.Y);

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
        DateTime endDate = dates.Last();

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
        _dataVisualization.PlotHeatProduction(OptimizationPlot, filteredResults, filteredHeatDemand);
        PlotCrosshair(filteredResults, filteredHeatDemand);
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