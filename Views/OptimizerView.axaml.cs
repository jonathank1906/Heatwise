using Avalonia.Controls;
using ScottPlot;
using System.Linq;
using System.Collections.Generic;
using Heatwise.ViewModels;
using Avalonia.Interactivity;
using System;
using Heatwise.Models;
using Avalonia;
using ScottPlot.Avalonia;
using System.Diagnostics;

namespace Heatwise.Views;

public partial class OptimizerView : UserControl
{
    private AvaPlot _plot;
    private bool _tooltipsEnabled = true;
    private Flyout? _calendarFlyout;
    private Flyout? _plotTypeFlyout;
    private ToolTipView? _tooltipWindow;
    private bool _hasAutoOpenedWindow = false;
    private ScottPlot.Plottables.Crosshair? _hoverCrosshair;
    private List<(DateTime timestamp, double value)>? _currentHeatDemandData;
    private List<HeatProductionResult>? _currentFilteredResults;
    private DataVisualization? _dataVisualization;
    public AssetManager? AssetManager { get; }
    public OptimizerView()
    {
        InitializeComponent();
        InitializeFlyoutEvents();
        App.ThemeChanged += OnThemeChanged;

        _plot = this.Find<AvaPlot>("OptimizationPlot")!;

        DataContextChanged += (sender, e) =>
        {
            if (DataContext is OptimizerViewModel vm)
            {
                _dataVisualization = new DataVisualization(vm.AssetManager);
                vm.UpdateXAxisTicks += (timestamps) =>
                {
                    _dataVisualization.SetXAxisTicks(OptimizationPlot.Plot, timestamps);
                    OptimizationPlot.Refresh();
                };

                vm.PlotOptimizationResults = (results, demand) =>
                {
                    if (results == null || demand == null || AllMachinesHaveZeroOutput(results))
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }
                    _currentFilteredResults = results;
                    _currentHeatDemandData = demand;

                    if (this.FindControl<Calendar>("OptimizationCalendar") is Calendar calendar)
                    {
                        InitializeCalendarWithDates(calendar, demand.Select(d => d.timestamp).ToList());
                    }

                    _dataVisualization.PlotHeatProduction(OptimizationPlot, results, demand);
                    PlotCrosshair(results, demand);
                };

                vm.PlotElectricityPrices = (prices) =>
                {
                    if (prices == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    var priceValues = prices.Select(p => p.price).ToList();
                    _dataVisualization.PlotElectricityPrice(OptimizationPlot, priceValues);

                    var dummyResults = prices.Select(p => new HeatProductionResult { Timestamp = p.timestamp }).ToList();
                    var dummyDemand = prices.Select(p => (p.timestamp, p.price)).ToList();
                    PlotCrosshair(dummyResults, dummyDemand);
                };

                vm.PlotExpenses = (results) =>
                {
                    if (results == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    _currentFilteredResults = results;
                    var dummyDemand = results.Select(r => (r.Timestamp, 0.0)).ToList();
                    _dataVisualization.PlotExpenses(OptimizationPlot, results);
                    PlotCrosshair(results, dummyDemand);
                };

                vm.PlotEmissions = (results) =>
                {
                    if (results == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    _currentFilteredResults = results;
                    var dummyDemand = results.Select(r => (r.Timestamp, 0.0)).ToList();
                    _dataVisualization.PlotEmissions(OptimizationPlot, results);
                    PlotCrosshair(results, dummyDemand);
                };

                vm.PlotElectricityConsumption = (results) =>
                {
                    if (results == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    _currentFilteredResults = results;
                    var dummyDemand = results.Select(r => (r.Timestamp, 0.0)).ToList();
                    _dataVisualization.PlotElectricityConsumption(OptimizationPlot, results);
                    PlotCrosshair(results, dummyDemand);
                };

                vm.PlotElectricityProduction = (results) =>
                {
                    if (results == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    _currentFilteredResults = results;
                    var dummyDemand = results.Select(r => (r.Timestamp, 0.0)).ToList();
                    _dataVisualization.PlotElectricityProduction(OptimizationPlot, results);
                    PlotCrosshair(results, dummyDemand);
                };

                vm.PlotFuelConsumption = (results) =>
                {
                    if (results == null)
                    {
                        ClearPlotAndEmptyCalendar();
                        return;
                    }

                    _currentFilteredResults = results;
                    var dummyDemand = results.Select(r => (r.Timestamp, 0.0)).ToList();
        #pragma warning disable CS0612 // Type or member is obsolete
                    _dataVisualization.PlotFuelConsumption(OptimizationPlot, results);
        #pragma warning restore CS0612 // Type or member is obsolete
                    PlotCrosshair(results, dummyDemand);
                };
            }
        };
    }

    private bool AllMachinesHaveZeroOutput(List<HeatProductionResult> results)
    {
        if (results == null) return true;

        return results
            .Where(r => r.AssetName != "Interval Summary")
            .All(r => r.HeatProduced <= 0);
    }
    private void OnThemeChanged()
    {
        if (_dataVisualization != null && OptimizationPlot != null)
        {
            var plt = OptimizationPlot.Plot;
            plt.Axes.Color(_dataVisualization.GetCurrentThemeAxesColor());
            plt.Legend.BackgroundColor = _dataVisualization.GetCurrentThemeBackgroundColor();
            plt.Legend.FontColor = _dataVisualization.GetCurrentThemeAxesColor();
            plt.Legend.OutlineColor = _dataVisualization.GetCurrentThemeAxesColor();
            OptimizationPlot.Refresh();
        }
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

        var groupedResults = results
            .Where(r => r.AssetName != "Interval Summary")
            .GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .ToList();

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

            int barIndex = (int)Math.Round(coordinates.X);

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
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.ElectricityPrices:
                        var electricityPrice = _currentHeatDemandData
                            .FirstOrDefault(h => h.timestamp == timestamp).value;

                        tooltip += $"Electricity Price: {electricityPrice:F2} DKK\n";
                        _hoverCrosshair.HorizontalLine.Position = electricityPrice;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.ProductionCosts:
                        var productionCost = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.ProductionCost);

                        tooltip += $"Production Cost: {productionCost:F2} DKK\n";
                        _hoverCrosshair.HorizontalLine.Position = productionCost;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.CO2Emissions:
                        var emissions = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.Emissions);

                        tooltip += $"CO2 Emissions: {emissions:F2} kg\n";
                        _hoverCrosshair.HorizontalLine.Position = emissions;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.ElectricityConsumption:
                        var electricityConsumption = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.ElectricityConsumption);

                        tooltip += $"Electricity Consumption: {electricityConsumption:F2} MWh\n";
                        _hoverCrosshair.HorizontalLine.Position = electricityConsumption;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.ElectricityProduction:
                        var electricityProduction = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.ElectricityProduction);

                        tooltip += $"Electricity Production: {electricityProduction:F2} MWh\n";
                        _hoverCrosshair.HorizontalLine.Position = electricityProduction;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    case OptimizerViewModel.GraphType.FuelConsumption:
                        var oilConsumption = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.OilConsumption);

                        var gasConsumption = _currentFilteredResults
                            .Where(r => r.Timestamp == timestamp)
                            .Sum(r => r.GasConsumption);

                        tooltip += $"Oil Consumption: {oilConsumption:F2} MWh\n";
                        tooltip += $"Gas Consumption: {gasConsumption:F2} MWh\n";

                        _hoverCrosshair.HorizontalLine.IsVisible = false;
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;

                    default:
                        tooltip += "No data available for this graph type.\n";
                        _hoverCrosshair.VerticalLine.Position = barIndex;
                        break;
                }

                _hoverCrosshair.IsVisible = true;

                if (!_hasAutoOpenedWindow && (_tooltipWindow == null))
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

    private void InitializeTooltipWindow()
    {
        if (_tooltipWindow == null)
        {
            if (DataContext is OptimizerViewModel viewModel && viewModel.PopupService != null)
            {
                var popupService = viewModel.PopupService;
                popupService.ShowPopup<ToolTipViewModel>();

                popupService.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(popupService.IsPopupVisible))
                    {
                        if (!popupService.IsPopupVisible)
                        {
                            _tooltipsEnabled = false;
                            if (_hoverCrosshair != null)
                            {
                                _hoverCrosshair.IsVisible = false;
                                OptimizationPlot.Refresh();
                            }
                        }
                        else
                        {
                            _tooltipsEnabled = true;
                        }
                    }
                };
            }
        }
    }

    private void ShowTooltipWindow()
    {
        InitializeTooltipWindow();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is OptimizerViewModel viewModel &&
            viewModel.PopupService?.IsPopupVisible == true)
        {
            viewModel.PopupService.ClosePopup();
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateTooltipContent(string text)
    {
        if (DataContext is OptimizerViewModel viewModel && viewModel.PopupService.PopupContent is ToolTipViewModel tooltipViewModel)
        {
            tooltipViewModel.TooltipText = text;
        }
    }

    private void ToggleTooltip_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as OptimizerViewModel;
        if (viewModel == null || !viewModel.HasOptimized) return;

        if (viewModel.PopupService?.IsPopupVisible != true)
        {
            ShowTooltipWindow();
            _tooltipsEnabled = true;
        }
        else
        {
            viewModel.PopupService.ClosePopup();
            _tooltipsEnabled = false;
            if (_hoverCrosshair != null)
            {
                _hoverCrosshair.IsVisible = false;
                OptimizationPlot.Refresh();
            }
        }
    }

    private void SetDateRange_Click(object? sender, RoutedEventArgs e)
    {
        if (OptimizationCalendar.SelectedDates == null || _currentHeatDemandData == null)
            return;

        SetRangeFromCalendar(OptimizationCalendar.SelectedDates);
        _calendarFlyout?.Hide();
        OptimizationPlot.Refresh();
    }

    private void CalendarFlyout_Opened(object? sender, EventArgs e)
    {
        if (DataContext is OptimizerViewModel vm)
        {
            if (!vm.HasOptimized)
            {
                _calendarFlyout?.Hide();
            }
            vm.ResetViewCommand.Execute(null);
        }
        OptimizationCalendar.SelectedDates?.Clear();
        OptimizationPlot.Refresh();
    }

    private void PlotTypeFlyout_Opened(object? sender, EventArgs e)
    {
        if (DataContext is OptimizerViewModel vm)
        {
            if (!vm.HasOptimized)
            {
                if (sender is Flyout flyout)
                {
                    flyout.Hide();
                }
            }
        }
    }

    private void InitializeFlyoutEvents()
    {
        _calendarFlyout = CalendarButton.Flyout as Flyout;
        _plotTypeFlyout = PlotTypeButton.Flyout as Flyout;
        if (_calendarFlyout != null)
        {
            _calendarFlyout.Opened += CalendarFlyout_Opened;
        }
        if (_plotTypeFlyout != null)
        {
            _plotTypeFlyout.Opened += PlotTypeFlyout_Opened;
        }
    }

    private void SetRangeFromCalendar(IList<DateTime> selectedDates)
    {
        if (DataContext is OptimizerViewModel vm)
        {
            vm.SelectedDates = selectedDates.ToList();
            vm.SetDateRangeCommand.Execute(null);

            if (_currentFilteredResults != null)
            {
                var filteredTimestamps = _currentFilteredResults
                    .Select(r => r.Timestamp)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                _dataVisualization?.SetXAxisTicks(OptimizationPlot.Plot, filteredTimestamps);
            }
            OptimizationPlot.Refresh();
        }
    }

    private void ClearPlotAndEmptyCalendar()
    {
        OptimizationPlot.Plot.Clear();
        OptimizationPlot.Refresh();

        if (this.FindControl<Calendar>("OptimizationCalendar") is Calendar calendar)
        {
            calendar.SelectedDates.Clear();
            calendar.BlackoutDates.Clear();
            calendar.DisplayDateStart = null;
            calendar.DisplayDateEnd = null;
            calendar.DisplayDate = DateTime.Today;
        }

        _currentFilteredResults = null;
        _currentHeatDemandData = null;
    }

    private void InitializeCalendarWithDates(Calendar? calendar, List<DateTime> dates)
    {
        if (calendar == null && this.FindControl<Calendar>("OptimizationCalendar") is Calendar defaultCalendar)
        {
            calendar = defaultCalendar;
        }

        if (calendar == null) return;

        calendar.SelectedDates.Clear();
        calendar.BlackoutDates.Clear();

        if (dates.Any())
        {
            calendar.DisplayDate = dates.First();

            var allDates = dates.Select(d => d.Date).Distinct().ToList();
            var minDate = allDates.Min();
            var maxDate = allDates.Max();

            calendar.DisplayDateStart = minDate;
            calendar.DisplayDateEnd = maxDate;

            var date = minDate;
            while (date <= maxDate)
            {
                if (!allDates.Contains(date))
                {
                    calendar.BlackoutDates.Add(new CalendarDateRange(date));
                }
                date = date.AddDays(1);
            }
        }
        else
        {
            calendar.DisplayDateStart = null;
            calendar.DisplayDateEnd = null;
        }
    }

    private async void OnExportButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is OptimizerViewModel viewModel)
        {
            Debug.WriteLine("Export button clicked.outer");
            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                Debug.WriteLine("Export button clicked.inner");
                await viewModel.ExportToCsv(parentWindow);
            }
        }
    }
}