using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Linq;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Interfaces;
using Sem2Proj.Enums;
using Avalonia.Controls;
using System.Windows.Input;

namespace Sem2Proj.ViewModels;

public partial class AssetManagerViewModel : ObservableObject
{

    [ObservableProperty]
    private ViewState _currentViewState = ViewState.ScenarioSelection;
    [ObservableProperty]
    private ICommand? _parentDeleteCommand;
    private Flyout? _calendarFlyout;
    private readonly IPopupService _popupService;
    [ObservableProperty]
    private ObservableCollection<AssetModel> _currentScenarioAssets = new();
    private readonly AssetManager _assetManager;

    [ObservableProperty]
    private bool _showScenarioSelection;

    [ObservableProperty]
    private bool _showAssetDetails = true;

    [ObservableProperty]
    private ObservableCollection<AssetModel> _displayedAssets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedAssets))]
    private string? _selectedScenario;

    [ObservableProperty]
    private AssetModel? _selectedAsset;

    [ObservableProperty]
    private Bitmap? _imageFromBinding;

    public string ImageSource { get; set; }

    [ObservableProperty]
    private Bitmap? _gridImageFromBinding;

    public ObservableCollection<string> AvailableScenarios { get; }

    public HeatingGrid? GridInfo => _assetManager.GridInfo;

    public AssetManagerViewModel(AssetManager assetManager, IPopupService popupService)
    {
        _assetManager = assetManager;
        _popupService = popupService;

        // Initialize scenarios list
        AvailableScenarios = new ObservableCollection<string>(
            new[] { "All Assets" }
                .Concat(_assetManager.Presets.Select(p => p.Name))
        );

        // Set default state
        SelectedScenario = "Scenario 1";

        // Load initial grid image if available
        if (GridInfo?.ImageSource != null)
        {
            LoadGridImageFromSource(GridInfo.ImageSource);
        }
    }
    [RelayCommand]
    private void NavigateTo(string destination)
    {
        if (destination == "Production Units")
        {
            CurrentViewState = ViewState.ScenarioSelection;
            return;
        }

        if (destination == "All Assets" || AvailableScenarios.Contains(destination))
        {
            SelectedScenario = destination == "All Assets" ? "All Assets" : destination;
            CurrentViewState = ViewState.AssetDetails;
        }
        else if (destination == "Configure")
        {
            CurrentViewState = ViewState.Configure;
        }
    }

    [RelayCommand]
    private void ShowConfiguration()
    {
        CurrentViewState = ViewState.Configure;
    }

    partial void OnSelectedAssetChanged(AssetModel? value)
    {
        if (value != null)
        {
            Debug.WriteLine($"Selected asset changed to: {value.Name}");
            LoadImageFromSource(value.ImageSource);
        }
    }

    partial void OnSelectedScenarioChanged(string? value)
    {
        if (value == "All Assets")
        {
            CurrentScenarioAssets = new ObservableCollection<AssetModel>(
                _assetManager.AllAssets.Select(a => new AssetModel
                {
                    Name = a.Name,
                    MaxHeat = a.MaxHeat,
                    ProductionCosts = a.ProductionCosts,
                    Emissions = a.Emissions,
                    GasConsumption = a.GasConsumption,
                    OilConsumption = a.OilConsumption,
                    MaxElectricity = a.MaxElectricity,
                    ImageFromBinding = LoadImageFromSource(a.ImageSource),
                    DeleteCommand = DeleteMachineCommand
                })
            );
        }
        else
        {
            var preset = _assetManager.Presets.FirstOrDefault(p => p.Name == value);
            CurrentScenarioAssets = preset != null
                ? new ObservableCollection<AssetModel>(
                    _assetManager.AllAssets
                        .Where(a => preset.Machines.Contains(a.Name))
                        .Select(a => new AssetModel
                        {
                            Name = a.Name,
                            MaxHeat = a.MaxHeat,
                            ProductionCosts = a.ProductionCosts,
                            Emissions = a.Emissions,
                            GasConsumption = a.GasConsumption,
                            OilConsumption = a.OilConsumption,
                            MaxElectricity = a.MaxElectricity,
                            ImageFromBinding = LoadImageFromSource(a.ImageSource),
                            DeleteCommand = DeleteMachineCommand
                        }))
                : new ObservableCollection<AssetModel>();
        }

        ShowScenarioSelection = false;
        ShowAssetDetails = true;
    }

    private Bitmap? LoadImageFromSource(string imageSource)
    {
        try
        {
            if (string.IsNullOrEmpty(imageSource)) return null;

            var normalizedPath = imageSource.TrimStart('/', '\\');
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, normalizedPath);

            if (File.Exists(fullPath))
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    return new Bitmap(stream);
                }
            }
            Debug.WriteLine($"Image not found at: {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading image: {ex.Message}");
        }
        return null;
    }

    private void LoadGridImageFromSource(string imageSource)
    {
        try
        {
            if (string.IsNullOrEmpty(imageSource))
            {
                GridImageFromBinding = null;
                return;
            }

            var normalizedPath = imageSource.TrimStart('/', '\\');
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, normalizedPath);

            if (File.Exists(fullPath))
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    GridImageFromBinding = new Bitmap(stream);
                }
            }
            else
            {
                Debug.WriteLine($"Grid image not found at: {fullPath}");
                GridImageFromBinding = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading grid image: {ex.Message}");
            GridImageFromBinding = null;
        }
    }
    [RelayCommand]
    public void ShowSettings()
    {
        var settingsViewModel = new CreateViewModel(_assetManager, _popupService);

        settingsViewModel.AssetCreatedSuccessfully += () =>
  {
      // This is the key fix - completely rebuild the collection
      var current = SelectedScenario;
      CurrentScenarioAssets = new ObservableCollection<AssetModel>(
          current == "All Assets"
              ? _assetManager.AllAssets.Select(CreateAssetModel)
              : _assetManager.Presets
                  .FirstOrDefault(p => p.Name == current)?
                  .Machines
                  .Select(m => _assetManager.AllAssets.FirstOrDefault(a => a.Name == m))
                  .Where(a => a != null)
                  .Select(CreateAssetModel)
                  ?? Enumerable.Empty<AssetModel>()
      );
  };



        _popupService.ShowPopup(settingsViewModel);
    }

    private AssetModel CreateAssetModel(AssetModel source)
    {
        return new AssetModel
        {
            Name = source.Name,
            MaxHeat = source.MaxHeat,
            ProductionCosts = source.ProductionCosts,
            Emissions = source.Emissions,
            GasConsumption = source.GasConsumption,
            OilConsumption = source.OilConsumption,
            MaxElectricity = source.MaxElectricity,
            ImageFromBinding = LoadImageFromSource(source.ImageSource),
            DeleteCommand = DeleteMachineCommand
        };
    }
    [RelayCommand]
    private void DeleteMachine(string machineName)
    {
        if (SelectedScenario == null || SelectedScenario == "All Assets") return;

        bool success = _assetManager.RemoveMachineFromPreset(SelectedScenario, machineName);

        if (success)
        {
            // Refresh the current view
            OnSelectedScenarioChanged(SelectedScenario);
            Events.Notification.Invoke($"Machine {machineName} removed from preset", NotificationType.Confirmation);
        }
        else
        {
            Events.Notification.Invoke($"Failed to remove machine {machineName}", NotificationType.Error);
        }
    }
}

public enum ViewState
{
    ScenarioSelection,
    AssetDetails,
    Configure
}