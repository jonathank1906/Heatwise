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
                vm.PlotEmissions = (results) =>
                    _dataVisualization.PlotEmissions(OptimizationPlot, results);
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
        // Removed call to PlotResults
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