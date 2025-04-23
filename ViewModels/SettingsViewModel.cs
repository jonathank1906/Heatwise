using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Interfaces;
using System.Diagnostics;
using Sem2Proj.Models;

namespace Sem2Proj.ViewModels;


public partial class SettingsViewModel : ViewModelBase, IPopupViewModel
{
    public event Action? AssetCreatedSuccessfully;
    private readonly AssetManager _assetManager;
    private readonly IPopupService _popupService;
    // Close command
    public ICommand CloseCommand { get; private set; }

    // Form properties
    [ObservableProperty]
    private string _machineName = string.Empty;

    [ObservableProperty]
    private string[] _machineTypes = ["Boiler", "CHP", "Heat Pump", "Electric Heater"];

    [ObservableProperty]
    private string? _selectedMachineType;

    [ObservableProperty]
    private string _maxHeatOutput = "0";

    [ObservableProperty]
    private string _maxElectricityOutput = "0";

    [ObservableProperty]
    private string _productionCost = "0";

    [ObservableProperty]
    private string _co2Emissions = "0";

    [ObservableProperty]
    private string _gasConsumption = "0";

    [ObservableProperty]
    private string _oilConsumption = "0";

    [ObservableProperty]
    private string? _imagePath;

    public SettingsViewModel(AssetManager assetManager, IPopupService popupService)
    {
        _assetManager = assetManager;
        _popupService = popupService;
        CloseCommand = new RelayCommand(() => _popupService.ClosePopup());
    }

    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }

    [RelayCommand]
    private void BrowseImage()
    {
        // Implement file browsing logic
    }

    [RelayCommand]
    private void CreateMachine()
    {
        // Validate numeric inputs
        if (!double.TryParse(MaxHeatOutput, out double maxHeat) ||
            !double.TryParse(MaxElectricityOutput, out double maxElectricity) ||
            !double.TryParse(ProductionCost, out double productionCost) ||
            !double.TryParse(Co2Emissions, out double emissions) ||
            !double.TryParse(GasConsumption, out double gasConsumption) ||
            !double.TryParse(OilConsumption, out double oilConsumption))
        {
            Debug.WriteLine("Invalid numeric input values");
            return;
        }

        if (string.IsNullOrWhiteSpace(MachineName))
        {
            Debug.WriteLine("Machine name cannot be empty");
            return;
        }



        bool success = _assetManager.CreateNewAsset(
            MachineName,
            ImagePath,
            maxHeat,
            maxElectricity,
            productionCost,
            emissions,
            gasConsumption,
            oilConsumption
        );

        if (success)
        {
            Debug.WriteLine($"Successfully created new asset '{MachineName}'");
            AssetCreatedSuccessfully?.Invoke();
            CloseCommand.Execute(null);
        }
        else
        {
            Debug.WriteLine("Failed to create new asset");
        }
    }
}