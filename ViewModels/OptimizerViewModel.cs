using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sem2Proj.Models;
using System.Diagnostics;
using Avalonia.Controls;
using System.Collections.ObjectModel;


namespace Sem2Proj.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{
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
    [RelayCommand]
    private void ApplyDateRange()
    {
        SetDateRange(); // Reuse the existing SetDateRange logic
    }

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
    private OptimisationMode _optimisationMode = OptimisationMode.Cost;

    // Side pane properties -------------------------------
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;

    [ObservableProperty]
    private double _paneWidth = OpenWidth;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isOpening;

    [ObservableProperty]
    private ObservableCollection<Preset> _availablePresets;

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


    public enum GraphType
    {
        HeatProduction,
        HeatDemand,
        ProductionCosts,
        ElectricityPrices,
        ElectricityProduction,
        ElectricityConsumption,
        FuelConsumption,
        CO2Emissions,
        ExpensesAndProfit
    }

    partial void OnSelectedGraphTypeChanged(GraphType value)
    {
        SwitchGraph(value);
    }

    public OptimizerViewModel(AssetManager assetManager, SourceDataManager sourceDataManager, ResultDataManager resultDataManager)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _sourceDataManager = sourceDataManager ?? throw new ArgumentNullException(nameof(sourceDataManager));
        _resultDataManager = resultDataManager ?? throw new ArgumentNullException(nameof(resultDataManager));
        _optimizer = new Optimizer(_assetManager, _sourceDataManager);
        AvailablePresets = new ObservableCollection<Preset>(_assetManager.Presets);

        // Set up the selection callback for each preset
        foreach (var preset in AvailablePresets)
        {
            preset.SetSelectPresetAction(SelectPreset);
        }

        // Select first preset by default if available
        if (AvailablePresets.Count > 0)
        {
            AvailablePresets[0].IsPresetSelected = true;
            _assetManager.SetScenario(0);
        }

    }

    [RelayCommand]
    private void SelectPreset(Preset preset)
    {
        if (preset == null) return;

        // Find the index of the selected preset
        var presetIndex = AvailablePresets.IndexOf(preset);
        if (presetIndex >= 0)
        {
            _assetManager.SetScenario(presetIndex);

            // Update selection states
            foreach (var p in AvailablePresets)
            {
                p.IsPresetSelected = false;
            }
            preset.IsPresetSelected = true;
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
        // Fetch all required data
        HeatDemandData = _sourceDataManager.GetData(SelectedDataType);
        HeatDemand = HeatDemandData.Sum(data => data.value);

        // Initialize electricity price data
        WinterElectricityPriceData = _sourceDataManager.GetWinterElectricityPriceData()
            .Select(x => x.value).ToList();
        SummerElectricityPriceData = _sourceDataManager.GetSummerElectricityPriceData()
            .Select(x => x.value).ToList();

        // Perform optimization
        OptimizationResults = _optimizer.CalculateOptimalHeatProduction(HeatDemandData, OptimisationMode);

        // Save and fetch results
        _resultDataManager.SaveResultsToDatabase(
            OptimizationResults.Where(r => r.AssetName != "Interval Summary").ToList()
        );
        OptimizationResults = _resultDataManager.GetLatestResults();

        // Ensure the graph is updated based on the selected graph type
        HasOptimized = true;
        SwitchGraph(SelectedGraphType); // Explicitly call SwitchGraph here
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
            }
        }
        else
        {
            // Use filtered data if dates are selected
            RefreshCurrentView();
        }
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


        // Depending on which graph is currently selected, reset that specific view
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
            OptimisationMode = OptimisationMode.Cost;
        }
    }

    partial void OnIsCO2SelectedChanged(bool value)
    {
        if (value)
        {
            OptimisationMode = OptimisationMode.CO2;
        }
    }

    // partial void OnIsScenario1SelectedChanged(bool value)
    // {
    //     if (value)
    //     {
    //         _assetManager.SetScenario(0); // Scenario 1
    //         Debug.WriteLine("Scenario 1 selected");
    //     }
    // }

    // partial void OnIsScenario2SelectedChanged(bool value)
    // {
    //     if (value)
    //     {
    //         _assetManager.SetScenario(1); // Scenario 2
    //         Debug.WriteLine("Scenario 2 selected");
    //     }
    // }

    [RelayCommand]
    public async Task ExportToCsv(Window parentWindow)
    {
        try
        {
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