using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sem2Proj.Models;
using System.Diagnostics;
using Avalonia.Controls;


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

    // Scenario radio buttons -------------------------------
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenario2Selected))]
    private bool _isScenario1Selected = true;

    [ObservableProperty]
    private bool _isScenario2Selected;

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
    public Action<List<double>>? PlotElectricityPrices { get; set; }
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

        _assetManager.SetScenario(0);
        _isScenario1Selected = true;
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
                    ? _sourceDataManager.GetWinterElectricityPriceData().Select(x => x.value).ToList()
                    : _sourceDataManager.GetSummerElectricityPriceData().Select(x => x.value).ToList();

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

    [RelayCommand]
    private void SetDateRange()
    {
        if (OptimizationResults == null || HeatDemandData == null || !HasOptimized) return;

        // Get selected dates from the calendar (passed via command parameter)
        if (SelectedDates == null || SelectedDates.Count == 0) return;

        var orderedDates = SelectedDates.OrderBy(d => d).ToList();
        DateTime startDate = orderedDates.First();
        DateTime endDate = orderedDates.Last().AddDays(1);

        // Filter both optimization results and heat demand data
        var filteredResults = OptimizationResults
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .ToList();

        var filteredHeatDemand = HeatDemandData
            .Where(h => h.timestamp >= startDate && h.timestamp <= endDate)
            .ToList();

        PlotOptimizationResults?.Invoke(filteredResults, filteredHeatDemand);
    }

    [RelayCommand]
    private void ResetView()
    {
        if (!HasOptimized) return;

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
                    ? _sourceDataManager.GetWinterElectricityPriceData().Select(x => x.value).ToList()
                    : _sourceDataManager.GetSummerElectricityPriceData().Select(x => x.value).ToList();
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

    partial void OnIsScenario1SelectedChanged(bool value)
    {
        if (value)
        {
            _assetManager.SetScenario(0); // Scenario 1
            Debug.WriteLine("Scenario 1 selected");
        }
    }

    partial void OnIsScenario2SelectedChanged(bool value)
    {
        if (value)
        {
            _assetManager.SetScenario(1); // Scenario 2
            Debug.WriteLine("Scenario 2 selected");
        }
    }

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