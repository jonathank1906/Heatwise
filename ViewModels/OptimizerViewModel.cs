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

namespace Sem2Proj.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{


    private readonly AssetManager _assetManager;


    // Scenario ratio buttons
 [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenario2Selected))]
    private bool _isScenario1Selected = true;
    
    [ObservableProperty]
    private bool _isScenario2Selected;

    // Radio button options
    [ObservableProperty]
    private bool isSummerSelected; // Tracks if "Summer" is selected

    [ObservableProperty]
    private bool isWinterSelected = true; // Default to "Winter"

    [ObservableProperty]
    private bool isCostSelected = true; // Default to "Cost"

    [ObservableProperty]
    private bool isCO2Selected; // Tracks if "CO2" is selected

    // SourceDataManager
    private readonly SourceDataManager _sourceDataManager;

    // Optimizer
    private readonly Optimizer _optimizer;

    [ObservableProperty]
    private SourceDataManager.DataType selectedDataType = SourceDataManager.DataType.WinterHeatDemand; // Default to WinterHeatDemand

    [ObservableProperty]
    private double heatDemand = 0.0; // Default value, can be updated via UI

    [ObservableProperty]
    private OptimisationMode optimisationMode = OptimisationMode.Cost; // Default mode

    [ObservableProperty]
    private List<HeatProductionResult> optimizationResults;

    // Action to trigger plot updates in the view
    public Action<string[], double[]>? PlotOptimizationResults { get; set; }

    // Side pane ------------------------------------------------
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;

    [ObservableProperty]
    private double _paneWidth = OpenWidth;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isOpening;

    [RelayCommand]
    private async Task TriggerPane()
    {
        IsOpening = !IsPaneOpen;
        IsPaneOpen = !IsPaneOpen;
        PaneWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
    }
    // -----------------------------------------------------------

    // Command to perform optimization and update the plot
    [RelayCommand]
    private void OptimizeAndPlot()
    {
        // Fetch heat demand dynamically based on the selected data type
        var heatDemandData = _sourceDataManager.GetData(SelectedDataType);
        HeatDemand = heatDemandData.Sum(data => data.value); // Sum all heat demand values

        // Perform optimization (Call optimizer.cs)
        OptimizationResults = _optimizer.CalculateOptimalHeatProduction(heatDemandData, OptimisationMode);

        // Prepare data for plotting
        var assetNames = OptimizationResults.Select(r => r.AssetName).ToArray();
        var heatProduced = OptimizationResults.Select(r => r.HeatProduced).ToArray();

        // Trigger plot update in the view
        PlotOptimizationResults?.Invoke(assetNames, heatProduced);
    }

    // Constructor
   public OptimizerViewModel(AssetManager assetManager, SourceDataManager sourceDataManager)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _sourceDataManager = sourceDataManager ?? throw new ArgumentNullException(nameof(sourceDataManager));
        _optimizer = new Optimizer(_assetManager, _sourceDataManager);
        
        // Initialize with default scenario
        _assetManager.SetScenario(0); // Default to Scenario 1
        _isScenario1Selected = true;
    }

    // Update the selected data type based on the radio button selection
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

}