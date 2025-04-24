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
using System.Collections.Generic;

namespace Sem2Proj.ViewModels;

public partial class AssetManagerViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<Preset> _availablePresets = new();
    [ObservableProperty]
    private bool isConfiguring;
    [ObservableProperty]
    private ObservableCollection<AssetModel> _allAssets;
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

    // First create the presets with their machine lists
    var presetTemplates = new ObservableCollection<Preset>(
        _assetManager.Presets.Select(p => new Preset
        {
            Id = p.Id,
            Name = p.Name,
            Machines = new List<string>(p.Machines), // Create a new list
            NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name))
        })
    );

    // Initialize scenarios list
    AvailableScenarios = new ObservableCollection<string>(
        new[] { "All Assets" }
            .Concat(presetTemplates.Select(p => p.Name))
    );

    // Set default state
    SelectedScenario = null;
    CurrentViewState = ViewState.PresetNavigation;

    // Load initial grid image if available
    if (GridInfo?.ImageSource != null)
    {
        LoadGridImageFromSource(GridInfo.ImageSource);
    }

    // Initialize assets with their own preset instances
    AllAssets = new ObservableCollection<AssetModel>(
        _assetManager.AllAssets.Select(a => 
        {
            var assetModel = new AssetModel
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
            };
            
            // Initialize with preset templates - this will set IsSelected correctly
            assetModel.InitializePresetSelections(presetTemplates);
            return assetModel;
        })
    );

    // Set the available presets
    AvailablePresets = presetTemplates;
}

    [RelayCommand]
    private void NavigateToPreset(string presetName)
    {
        NavigateTo(presetName);
    }

    [RelayCommand]
    public void NavigateTo(string destination)
    {
        if (destination == "All Assets")
        {
            CurrentViewState = ViewState.AssetDetails;
            SelectedScenario = "All Assets";
        }
        else if (_availablePresets.Any(p => p.Name == destination))
        {
            CurrentViewState = ViewState.AssetDetails;
            SelectedScenario = destination;
        }
        else if (destination == "PresetNavigation")
        {
            CurrentViewState = ViewState.PresetNavigation;
            SelectedScenario = null;
        }
        else if (destination == "Presets")
        {
            CurrentViewState = ViewState.PresetNavigation;
            SelectedScenario = null;
        }
        else
        {
            CurrentViewState = ViewState.ScenarioSelection;
            SelectedScenario = null;
        }
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
_assetManager.RefreshAssets();
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
      AvailablePresets = new ObservableCollection<Preset>(
            _assetManager.Presets.Select(p => new Preset
            {
                Name = p.Name,
                Machines = p.Machines,
                NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name))
            })
        );
  };

  RefreshPresets();

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


    [RelayCommand]
    private void ShowConfiguration()
    {
        CurrentViewState = ViewState.Configure;
        // Refresh the presets to ensure the latest data is shown
        RefreshPresets();
    
    // Also refresh individual assets' preset selections
    foreach (var asset in AllAssets)
    {
        asset.InitializePresetSelections(AvailablePresets);
    }
       Debug.WriteLine("Refreshing presets for configuration view.");
        IsConfiguring = true;
    }


    [RelayCommand]
    private void CancelConfiguration()
    {
        IsConfiguring = false;
        CurrentViewState = ViewState.PresetNavigation;
    }

    [RelayCommand]
    private void SaveConfiguration()
    {
        // Add logic to save configuration changes
        IsConfiguring = false;
    }

    partial void OnAvailablePresetsChanged(ObservableCollection<Preset> value)
    {
        // Notify UI when presets change
        OnPropertyChanged(nameof(AvailablePresets));
    }

    // Refresh the presets list dynamically
   public void RefreshPresets()
{
    // First refresh the underlying data
    _assetManager.RefreshAssets();
    
    // Then update our local collection
    AvailablePresets = new ObservableCollection<Preset>(
        _assetManager.Presets.Select(p => new Preset
        {
            Id = p.Id,
            Name = p.Name,
            Machines = new List<string>(p.Machines),
            NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name))
        })
    );
    
    Debug.WriteLine($"Refreshed presets. Now have {AvailablePresets.Count} presets.");
}
}

public enum ViewState
{
    ScenarioSelection,
    AssetDetails,
    Configure,
    PresetNavigation
}