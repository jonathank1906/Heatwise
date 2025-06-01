using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heatwise.Models;
using System.Diagnostics;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using Heatwise.Interfaces;
using Heatwise.Enums;


namespace Heatwise.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{
    public IPopupService PopupService { get; }

    public AssetManager AssetManager => _assetManager;
    [ObservableProperty]
    private bool _hasOptimized = false;

    [ObservableProperty]
    private bool _showHeatDemand = true;

    [ObservableProperty]
    private List<HeatProductionResult>? _optimizationResults;

    [ObservableProperty]
    private List<(DateTime timestamp, double value)>? _heatDemandData;

    private readonly AssetManager _assetManager;
    private readonly SourceDataManager _sourceDataManager;
    private readonly Optimizer _optimizer;
    private readonly ResultDataManager _resultDataManager;

    public event Action<List<DateTime>>? UpdateXAxisTicks;


    [ObservableProperty]
    private List<HeatProductionResult>? _filteredOptimizationResults;

    [ObservableProperty]
    private List<(DateTime timestamp, double value)>? _filteredHeatDemandData;

    [ObservableProperty]
    private List<(DateTime timestamp, double price)>? _filteredElectricityPriceData;


    [ObservableProperty]
    private IList<DateTime>? _selectedDates;

    [ObservableProperty]
    private SourceDataManager.DataType _selectedDataType = SourceDataManager.DataType.WinterHeatDemand;

    [ObservableProperty]
    private double _heatDemand = 0.0;

    [ObservableProperty]
    private OptimizationMode _optimizationMode = OptimizationMode.Cost;

    // Side pane properties -------------------------------
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;

    [ObservableProperty]
    private double _paneWidth = OpenWidth;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isOpening;

    public ObservableCollection<Preset> Presets => _assetManager.Presets;

    // Scenario radio buttons -------------------------------
    [ObservableProperty]
    private bool _isSummerSelected;

    [ObservableProperty]
    private bool _isWinterSelected = true;

    [ObservableProperty]
    private bool _isCostSelected = true;

    [ObservableProperty]
    private bool _isCO2Selected;

    [ObservableProperty]
    private GraphType _selectedGraphType = GraphType.HeatProduction;

    [ObservableProperty]
    private List<double>? _winterElectricityPriceData;

    [ObservableProperty]
    private List<double>? _summerElectricityPriceData;
    public Action<List<HeatProductionResult>, List<(DateTime timestamp, double value)>>? PlotOptimizationResults { get; set; }
    public Action<List<(DateTime timestamp, double price)>>? PlotElectricityPrices { get; set; }
    public Action<List<HeatProductionResult>>? PlotExpenses { get; set; }
    public Action<List<HeatProductionResult>>? PlotEmissions { get; set; }
    public Action<List<HeatProductionResult>>? PlotElectricityConsumption { get; set; }
    public Action<List<HeatProductionResult>>? PlotElectricityProduction { get; set; }
    public Action<List<HeatProductionResult>>? PlotFuelConsumption { get; set; }



    partial void OnSelectedGraphTypeChanged(GraphType value)
    {
        SwitchGraph(value);
    }

    public OptimizerViewModel(AssetManager assetManager, SourceDataManager sourceDataManager, ResultDataManager resultDataManager, IPopupService popupService)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _sourceDataManager = sourceDataManager ?? throw new ArgumentNullException(nameof(sourceDataManager));
        _resultDataManager = resultDataManager ?? throw new ArgumentNullException(nameof(resultDataManager));
        _optimizer = new Optimizer(_assetManager);
        PopupService = popupService;

        // Set up the selection callback for each preset
        foreach (var preset in _assetManager.Presets)
        {
            preset.SetSelectPresetAction(SelectPreset);
        }

        // Select first preset by default if available
        if (_assetManager.Presets.Count > 0)
        {
            _assetManager.SetScenario(0);
        }

        // Subscribe to preset changes
        _assetManager.Presets.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Presets));
            if (e.NewItems != null)
            {
                foreach (Preset preset in e.NewItems)
                {
                    preset.SetSelectPresetAction(SelectPreset);
                }
            }
        };
    }

    [RelayCommand]
    private void SelectPreset(Preset preset)
    {
        var presetIndex = _assetManager.Presets.IndexOf(preset);
        if (presetIndex >= 0)
        {
            _assetManager.SetScenario(presetIndex);
        }
    }

    [RelayCommand]
    private void TriggerPane()
    {
        IsOpening = !IsPaneOpen;
        IsPaneOpen = !IsPaneOpen;
        PaneWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
    }

    [RelayCommand]
    private void OptimizeAndPlot()
    {
        // This is important to ensure a preset is always selected
        _assetManager.SetScenario(_assetManager.SelectedScenarioIndex);
        // Check if the selected preset has any active machines
        var activeMachines = _assetManager.GetActiveMachinesForCurrentPreset();

        if (activeMachines == null || !activeMachines.Any() || activeMachines.All(m => m.HeatProduction <= 0))
        {
            // Clear all plots and reset state
            OptimizationResults = null;
            HeatDemandData = null;
            HasOptimized = false;

            // Clear all plots by invoking them with null data
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            PlotOptimizationResults?.Invoke(null, null);
            PlotElectricityPrices?.Invoke(null);
            PlotExpenses?.Invoke(null);
            PlotEmissions?.Invoke(null);
            PlotElectricityConsumption?.Invoke(null);
            PlotElectricityProduction?.Invoke(null);
            PlotFuelConsumption?.Invoke(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return;
        }
        // Fetch all required data
        HeatDemandData = _sourceDataManager.GetData(SelectedDataType);
        HeatDemand = HeatDemandData.Sum(data => data.value);

        // Initialize electricity price data
        WinterElectricityPriceData = _sourceDataManager.GetWinterElectricityPriceData()
            .Select(x => x.value).ToList();
        SummerElectricityPriceData = _sourceDataManager.GetSummerElectricityPriceData()
            .Select(x => x.value).ToList();

        // Select the appropriate electricity price data
        var selectedElectricityPriceData = IsWinterSelected
            ? _sourceDataManager.GetWinterElectricityPriceData()
            : _sourceDataManager.GetSummerElectricityPriceData();

        // Perform optimization
        OptimizationResults = _optimizer.CalculateOptimalHeatProduction(HeatDemandData, OptimizationMode, selectedElectricityPriceData);

        // Save and fetch results
        _resultDataManager.SaveResultsToDatabase(
            OptimizationResults.Where(r => r.AssetName != "Interval Summary").ToList()
        );
        OptimizationResults = _resultDataManager.GetLatestResults();

        // Ensure the graph is updated based on the selected graph type
        HasOptimized = true;
        SwitchGraph(SelectedGraphType);
    }


    public void SwitchGraph(GraphType graphType)
    {
        if (!HasOptimized) return;

        // Always use full data when resetting (SelectedDates is null)
        if (SelectedDates == null || SelectedDates.Count == 0)
        {
            switch (graphType)
            {
                case GraphType.HeatProduction:
                    if (OptimizationResults != null && HeatDemandData != null)
                    {
                        PlotOptimizationResults?.Invoke(OptimizationResults, HeatDemandData);
                    }
                    break;

                case GraphType.ElectricityPrices:
                    var electricityData = IsWinterSelected
                        ? _sourceDataManager.GetWinterElectricityPriceData()
                        : _sourceDataManager.GetSummerElectricityPriceData();
                    PlotElectricityPrices?.Invoke(electricityData);
                    break;

                case GraphType.ProductionCosts:
                    if (OptimizationResults != null)
                    {
                        PlotExpenses?.Invoke(OptimizationResults);
                    }
                    break;

                case GraphType.CO2Emissions:
                    if (OptimizationResults != null)
                    {
                        PlotEmissions?.Invoke(OptimizationResults);
                    }
                    break;
                case GraphType.ElectricityConsumption:
                    if (OptimizationResults != null)
                    {
                        PlotElectricityConsumption?.Invoke(OptimizationResults);
                    }
                    break;
                case GraphType.ElectricityProduction:
                    if (OptimizationResults != null)
                    {
                        PlotElectricityProduction?.Invoke(OptimizationResults);
                    }
                    break;
                case GraphType.FuelConsumption:
                    if (OptimizationResults != null)
                    {
                        PlotFuelConsumption?.Invoke(OptimizationResults);
                    }
                    break;
            }
        }
        else
        {
            // Use filtered data if dates are selected
            RefreshCurrentView();
        }

    }

    [RelayCommand]
    private void ApplyDateRange()
    {
        SetDateRange();
    }

    [RelayCommand]
    private void SetDateRange()
    {
        if (OptimizationResults == null || HeatDemandData == null || !HasOptimized) return;

        // If no dates are selected, reset to full data
        if (SelectedDates == null || SelectedDates.Count == 0)
        {
            FilteredOptimizationResults = OptimizationResults;
            FilteredHeatDemandData = HeatDemandData;

            // Get fresh electricity data based on season
            var electricityData = IsWinterSelected
                ? _sourceDataManager.GetWinterElectricityPriceData()
                : _sourceDataManager.GetSummerElectricityPriceData();
            FilteredElectricityPriceData = electricityData;

            // Update the current view
            RefreshCurrentView();
            return;
        }

        var orderedDates = SelectedDates.OrderBy(d => d).ToList();
        DateTime startDate = orderedDates.First();
        DateTime endDate = orderedDates.Last().AddDays(1);

        // Always filter all data sets, but only show the current view
        FilteredOptimizationResults = OptimizationResults
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();

        FilteredHeatDemandData = HeatDemandData
            .Where(h => h.timestamp >= startDate && h.timestamp <= endDate)
            .ToList();

        var electricityPriceData = (IsWinterSelected
            ? _sourceDataManager.GetWinterElectricityPriceData()
            : _sourceDataManager.GetSummerElectricityPriceData())
            .Where(p => p.timestamp >= startDate && p.timestamp <= endDate)
            .ToList();
        FilteredElectricityPriceData = electricityPriceData;

        // Update the current view
        RefreshCurrentView();
    }

    private void RefreshCurrentView()
    {
        switch (SelectedGraphType)
        {
            case GraphType.HeatProduction:
                if (FilteredOptimizationResults != null && FilteredHeatDemandData != null)
                {
                    PlotOptimizationResults?.Invoke(FilteredOptimizationResults, FilteredHeatDemandData);
                }
                break;

            case GraphType.ElectricityPrices:
                if (FilteredElectricityPriceData != null)
                {
                    PlotElectricityPrices?.Invoke(FilteredElectricityPriceData);
                }
                break;

            case GraphType.ProductionCosts:
                if (FilteredOptimizationResults != null)
                {
                    PlotExpenses?.Invoke(FilteredOptimizationResults);
                }
                break;

            case GraphType.CO2Emissions:
                if (FilteredOptimizationResults != null)
                {
                    PlotEmissions?.Invoke(FilteredOptimizationResults);
                }
                break;

            case GraphType.ElectricityConsumption:
                if (OptimizationResults != null)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    PlotElectricityConsumption?.Invoke(FilteredOptimizationResults);
#pragma warning restore CS8604 // Possible null reference argument.
                }
                break;

            case GraphType.ElectricityProduction:
                if (FilteredOptimizationResults != null)
                {
                    PlotElectricityProduction?.Invoke(FilteredOptimizationResults);
                }
                break;
            case GraphType.FuelConsumption:
                if (FilteredOptimizationResults != null)
                {
                    PlotFuelConsumption?.Invoke(FilteredOptimizationResults);
                }
                break;
        }
    }

    [RelayCommand]
    private void ResetView()
    {
        if (!HasOptimized) return;

        if (OptimizationResults != null)
        {
            var timestamps = OptimizationResults
                .Select(r => r.Timestamp)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            UpdateXAxisTicks?.Invoke(timestamps); // Add this event
        }

        switch (SelectedGraphType)
        {
            case GraphType.HeatProduction:
                if (OptimizationResults != null && HeatDemandData != null)
                {
                    PlotOptimizationResults?.Invoke(
                        OptimizationResults.Where(r => r.AssetName != "Interval Summary").ToList(),
                        HeatDemandData
                    );
                }
                break;

            case GraphType.ElectricityPrices:
                var electricityData = IsWinterSelected
                    ? _sourceDataManager.GetWinterElectricityPriceData()
                    : _sourceDataManager.GetSummerElectricityPriceData();
                PlotElectricityPrices?.Invoke(electricityData);
                break;

            case GraphType.ProductionCosts:
                if (OptimizationResults != null)
                {
                    PlotExpenses?.Invoke(OptimizationResults);
                }
                break;

            case GraphType.CO2Emissions:
                if (OptimizationResults != null)
                {
                    PlotEmissions?.Invoke(OptimizationResults);
                }
                break;
            case GraphType.ElectricityConsumption:
                if (OptimizationResults != null)
                {
                    PlotElectricityConsumption?.Invoke(OptimizationResults);
                }
                break;

            case GraphType.ElectricityProduction:
                if (OptimizationResults != null)
                {
                    PlotElectricityProduction?.Invoke(OptimizationResults);
                }
                break;
            case GraphType.FuelConsumption:
                if (OptimizationResults != null)
                {
                    PlotFuelConsumption?.Invoke(OptimizationResults);
                }
                break;
            // Add cases for other graph types as needed
            default:
                // Default to heat production if no specific case matches
                if (OptimizationResults != null && HeatDemandData != null)
                {
                    PlotOptimizationResults?.Invoke(
                        OptimizationResults.Where(r => r.AssetName != "Interval Summary").ToList(),
                        HeatDemandData
                    );

                }
                break;
        }
        SelectedDates?.Clear();
    }

    partial void OnIsSummerSelectedChanged(bool value)
    {
        if (value)
        {
            SelectedDataType = SourceDataManager.DataType.SummerHeatDemand;
        }
    }

    partial void OnIsWinterSelectedChanged(bool value)
    {
        if (value)
        {
            SelectedDataType = SourceDataManager.DataType.WinterHeatDemand;
        }
    }

    partial void OnIsCostSelectedChanged(bool value)
    {
        if (value)
        {
            OptimizationMode = OptimizationMode.Cost;
        }
    }

    partial void OnIsCO2SelectedChanged(bool value)
    {
        if (value)
        {
            OptimizationMode = OptimizationMode.CO2;
        }
    }

    [RelayCommand]
    public async Task ExportToCsv(Window parentWindow)
    {
        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var dialog = new SaveFileDialog
            {
                Title = "Export Optimization Results",
                Filters = new List<FileDialogFilter>
            {
                new() { Name = "CSV Files", Extensions = new() { "csv" } },
                new() { Name = "All Files", Extensions = new() { "*" } }
            },
                DefaultExtension = "csv",
                InitialFileName = $"optimization_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
#pragma warning restore CS0618 // Type or member is obsolete

            var result = await dialog.ShowAsync(parentWindow);
            if (result != null)
            {
                _resultDataManager.ExportToCsv(result);

                // // Optional: Show success notification
                // await ShowNotificationAsync("Export Successful", 
                //     $"Results were saved to {Path.GetFileName(result)}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Export failed: {ex.Message}");
            //await ShowErrorAsync("Export Failed", ex.Message);
        }
    }
}