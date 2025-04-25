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
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

namespace Sem2Proj.ViewModels;

public partial class AssetManagerViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<Preset> _availablePresets = new();
    [ObservableProperty]
    private bool isConfiguring = false;
    [ObservableProperty]
    private ObservableCollection<AssetModel> _allAssets;
    [ObservableProperty]
    private ViewState _currentViewState = ViewState.ScenarioSelection;
    [ObservableProperty]
    private ICommand? _parentRemoveFromPresetCommand;
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

    // ---------------------------------------------------------------------------------------------




    [ObservableProperty]
    private Preset? _selectedPreset;

    [ObservableProperty]
    private string _presetName = string.Empty;

    public event Action? AssetCreatedSuccessfully;




    // Form properties
    [ObservableProperty]
    private string _machineName = string.Empty;

    [ObservableProperty]
    private string? _imagePath;

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

    //----------------------------------------------------------------------------------------------

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
                    Id = a.Id,
                    Name = a.Name,
                    MaxHeat = a.MaxHeat,
                    ProductionCosts = a.ProductionCosts,
                    Emissions = a.Emissions,
                    GasConsumption = a.GasConsumption,
                    OilConsumption = a.OilConsumption,
                    MaxElectricity = a.MaxElectricity,
                    ImageFromBinding = LoadImageFromSource(a.ImageSource),
                    RemoveFromPresetCommand = RemoveFromPresetCommand,
                    DeleteCommand = DeleteCommand
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
                    Id = a.Id,
                    Name = a.Name,
                    MaxHeat = a.MaxHeat,
                    ProductionCosts = a.ProductionCosts,
                    Emissions = a.Emissions,
                    GasConsumption = a.GasConsumption,
                    OilConsumption = a.OilConsumption,
                    MaxElectricity = a.MaxElectricity,
                    ImageFromBinding = LoadImageFromSource(a.ImageSource),
                    RemoveFromPresetCommand = RemoveFromPresetCommand,
                    DeleteCommand = DeleteCommand
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
                            Id = a.Id,
                            Name = a.Name,
                            MaxHeat = a.MaxHeat,
                            ProductionCosts = a.ProductionCosts,
                            Emissions = a.Emissions,
                            GasConsumption = a.GasConsumption,
                            OilConsumption = a.OilConsumption,
                            MaxElectricity = a.MaxElectricity,
                            ImageFromBinding = LoadImageFromSource(a.ImageSource),
                            RemoveFromPresetCommand = RemoveFromPresetCommand,
                            DeleteCommand = DeleteCommand
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

            // Remove leading slash if present
            var cleanPath = imageSource.TrimStart('/');

            // Get the project root directory
            var projectDir = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

            // Combine with the image path
            var fullPath = Path.Combine(projectDir, cleanPath.Replace("/", "\\"));

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
            Debug.WriteLine($"Error loading image from {imageSource}: {ex.Message}");
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
        CurrentViewState = ViewState.Create;



        AssetCreatedSuccessfully += () =>
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

        CurrentViewState = ViewState.Create;
    }

    private AssetModel CreateAssetModel(AssetModel source)
    {
        var model = new AssetModel
        {
            Id = source.Id,
            Name = source.Name,
            ImageSource = source.ImageSource, // Ensure this is set
            MaxHeat = source.MaxHeat,
            ProductionCosts = source.ProductionCosts,
            Emissions = source.Emissions,
            GasConsumption = source.GasConsumption,
            OilConsumption = source.OilConsumption,
            MaxElectricity = source.MaxElectricity,
            RemoveFromPresetCommand = RemoveFromPresetCommand,
            DeleteCommand = DeleteCommand
        };

        // Load the image after all properties are set
        model.ImageFromBinding = LoadImageFromSource(model.ImageSource);

        model.InitializePresetSelections(AvailablePresets);
        return model;

    }
    [RelayCommand]
    private void RemoveFromPreset(string machineName)
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
    private void Delete(int machineId)
    {
        bool success = _assetManager.DeleteMachine(machineId);

        if (success)
        {
            // Refresh the UI
            var machine = AllAssets.FirstOrDefault(a => a.Id == machineId);
            if (machine != null)
            {
                AllAssets.Remove(machine);
                Events.Notification.Invoke("Machine deleted successfully", NotificationType.Confirmation);
            }
        }
        else
        {
            Events.Notification.Invoke("Failed to delete machine", NotificationType.Error);
        }
    }


    [RelayCommand]
    private void ShowConfiguration()
    {
        CurrentViewState = ViewState.Configure;
        // Refresh the presets to ensure the latest data is shown
        RefreshPresets();

        AllAssets = new ObservableCollection<AssetModel>(
        _assetManager.AllAssets.Select(a => new AssetModel
        {
            Id = a.Id,
            Name = a.Name,
            MaxHeat = a.MaxHeat,
            ProductionCosts = a.ProductionCosts,
            Emissions = a.Emissions,
            GasConsumption = a.GasConsumption,
            OilConsumption = a.OilConsumption,
            MaxElectricity = a.MaxElectricity,
            ImageFromBinding = LoadImageFromSource(a.ImageSource),
            RemoveFromPresetCommand = RemoveFromPresetCommand,
            DeleteCommand = DeleteCommand
        })
    );

        // Also refresh individual assets' preset selections
        foreach (var asset in AllAssets)
        {
            asset.InitializePresetSelections(AvailablePresets);
        }
        Debug.WriteLine("Refreshing presets for configuration view.");

    }


    [RelayCommand]
    private void CancelConfiguration()
    {

        CurrentViewState = ViewState.PresetNavigation;
    }

    partial void OnCurrentViewStateChanged(ViewState value)
    {
        // Set IsConfiguring to false only for PresetNavigation and AssetDetails
        IsConfiguring = !(value == ViewState.PresetNavigation || value == ViewState.AssetDetails);
    }

    [RelayCommand]
    private void SaveConfiguration()
    {
        try
        {
            Debug.WriteLine("=== Starting configuration save ===");

            // Log all assets with their current values
            Debug.WriteLine($"Found {AllAssets.Count} assets to save:");
            foreach (var asset in AllAssets)
            {
                Debug.WriteLine($"\nAsset: {asset.Name}");
                Debug.WriteLine($"- MaxHeat: {asset.MaxHeat}");
                Debug.WriteLine($"- ProductionCosts: {asset.ProductionCosts}");
                Debug.WriteLine($"- Emissions: {asset.Emissions}");
                Debug.WriteLine($"- GasConsumption: {asset.GasConsumption}");
                Debug.WriteLine($"- OilConsumption: {asset.OilConsumption}");
                Debug.WriteLine($"- MaxElectricity: {asset.MaxElectricity}");
                Debug.WriteLine($"- ImageSource: {asset.ImageSource ?? "null"}");

                // Also log preset assignments
                if (asset.AvailablePresets != null && asset.AvailablePresets.Any())
                {
                    Debug.WriteLine("Preset Assignments:");
                    foreach (var preset in asset.AvailablePresets)
                    {
                        Debug.WriteLine($"  - {preset.Name}: {(preset.IsSelected ? "Assigned" : "Not Assigned")}");
                    }
                }
            }

            // 1. Save all asset modifications
            bool allAssetsSaved = true;
            foreach (var asset in AllAssets)
            {
                Debug.WriteLine($"\nAttempting to save asset: {asset.Name}");
                if (!SaveAssetChanges(asset))
                {
                    allAssetsSaved = false;
                    Debug.WriteLine($"!! FAILED to save asset: {asset.Name} !!");
                }
                else
                {
                    Debug.WriteLine($"Successfully saved asset: {asset.Name}");
                }
            }

            // 2. Update preset assignments
            Debug.WriteLine("\nUpdating preset assignments...");
            bool presetsUpdated = UpdatePresetAssignments();
            Debug.WriteLine($"Preset assignments update {(presetsUpdated ? "succeeded" : "failed")}");

            RefreshPresets();

            // Also refresh individual assets' preset selections
            foreach (var asset in AllAssets)
            {
                asset.InitializePresetSelections(AvailablePresets);
            }

            if (allAssetsSaved && presetsUpdated)
            {

                Events.Notification.Invoke("Configuration saved successfully!", NotificationType.Confirmation);
                Debug.WriteLine("\n=== Configuration saved successfully ===");
            }
            else
            {
                Events.Notification.Invoke("Some changes failed to save.", NotificationType.Warning);
                Debug.WriteLine("\n=== Partial save completed with some failures ===");
            }
            CurrentViewState = ViewState.PresetNavigation;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"\n!!! Save failed: {ex.Message} !!!");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            Events.Notification.Invoke("Failed to save configuration!", NotificationType.Error);
        }

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



    private bool SaveAssetChanges(AssetModel asset)
    {
        try
        {
            // Update the asset in database without modifying the image source
            return _assetManager.UpdateAsset(
                asset.Id,
                asset.Name,
                string.Empty, // We're not updating the image source
                asset.MaxHeat,
                asset.MaxElectricity,
                asset.ProductionCosts,
                asset.Emissions,
                asset.GasConsumption,
                asset.OilConsumption
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save {asset.Name}: {ex.Message}");
            return false;
        }
    }

    private bool UpdatePresetAssignments()
    {
        try
        {
            Debug.WriteLine("Starting preset assignments update...");
            bool allUpdatesSuccessful = true;

            foreach (var asset in AllAssets)
            {
                Debug.WriteLine($"Updating presets for asset ID {asset.Id} ({asset.Name})");

                foreach (var preset in asset.AvailablePresets)
                {
                    bool shouldBeInPreset = preset.IsSelected;
                    bool isInPreset = _assetManager.IsMachineInPreset(preset.Id, asset.Id);

                    Debug.WriteLine($"- Preset ID {preset.Id} ({preset.Name}): " +
                                  $"Current={isInPreset}, Desired={shouldBeInPreset}");

                    if (shouldBeInPreset && !isInPreset)
                    {
                        Debug.WriteLine($"  Adding to preset ID {preset.Id}");
                        if (!_assetManager.AddMachineToPreset(preset.Id, asset.Id))
                        {
                            Debug.WriteLine($"  !! Failed to add to preset ID {preset.Id}");
                            allUpdatesSuccessful = false;
                        }
                    }
                    else if (!shouldBeInPreset && isInPreset)
                    {
                        Debug.WriteLine($"  Removing from preset ID {preset.Id}");
                        if (!_assetManager.RemoveMachineFromPreset(preset.Id, asset.Id))
                        {
                            Debug.WriteLine($"  !! Failed to remove from preset ID {preset.Id}");
                            allUpdatesSuccessful = false;
                        }
                    }
                }
            }

            if (allUpdatesSuccessful)
            {
                Debug.WriteLine("All preset assignments updated successfully");
            }
            else
            {
                Debug.WriteLine("Some preset assignments failed to update");
            }

            return allUpdatesSuccessful;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!! Failed to update presets: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            return false;
        }
    }
    //------------------------------------------------------------------------------------------------------------------------
    [RelayCommand]
    public async Task BrowseImage(Control view)
    {
        var topLevel = TopLevel.GetTopLevel(view);
        if (topLevel == null)
        {
            Debug.WriteLine("Unable to get TopLevel");
            return;
        }

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Machine Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" },
                    MimeTypes = new[] { "image/jpeg", "image/png" }
                }
            }
            });

            if (files.Count > 0 && files[0] is IStorageFile file)
            {
                var filePath = file.Path.LocalPath;

                // Get the project's Assets folder (source directory)
                var projectDir = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..")); // Go up from bin/Debug/net9.0
                var assetsDir = Path.Combine(projectDir, "Assets");
                Directory.CreateDirectory(assetsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                var destinationPath = Path.Combine(assetsDir, fileName);

                File.Copy(filePath, destinationPath, true);

                // Store the path relative to project root
                ImagePath = "/Assets/" + fileName;

                Debug.WriteLine($"Image saved to project folder: {destinationPath}");
                Debug.WriteLine($"ImagePath set to: {ImagePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Image selection error: {ex.Message}");
            Events.Notification.Invoke("Failed to select image", Enums.NotificationType.Error);
        }
    }

    [RelayCommand]
    public async Task CreateMachine(Control view)
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

        var selectedPresetNames = AvailablePresets
          .Where(p => p.IsSelected)
          .Select(p => p.Name)
          .ToList();

        bool success = _assetManager.CreateNewAsset(
            MachineName,
            ImagePath,
            maxHeat,
            maxElectricity,
            productionCost,
            emissions,
            gasConsumption,
            oilConsumption,
            selectedPresetNames
        );

        if (success)
        {
            Debug.WriteLine($"Successfully created new asset '{MachineName}'");
            _assetManager.RefreshAssets();


            CurrentViewState = ViewState.PresetNavigation;
        }
        else
        {
            Debug.WriteLine("Failed to create new asset");
        }
    }

    [RelayCommand]
    private void CreatePreset()
    {
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            Debug.WriteLine("Preset name cannot be empty.");
            Events.Notification.Invoke("Preset name cannot be empty.", Enums.NotificationType.Error);
            return;
        }

        bool success = _assetManager.CreateNewPreset(PresetName);
        if (success)
        {
            Debug.WriteLine($"Successfully created new preset '{PresetName}'");
            Events.Notification.Invoke($"Preset '{PresetName}' created successfully.", Enums.NotificationType.Confirmation);
            RefreshPresetList();
            AssetCreatedSuccessfully?.Invoke();

        }
        else
        {
            Debug.WriteLine("Failed to create new preset");
            Events.Notification.Invoke("Failed to create new preset.", Enums.NotificationType.Error);
        }
    }

    private void RefreshPresetList()
    {
        // Refresh from AssetManager
        _assetManager.RefreshAssets();

        // Update local AvailablePresets
        AvailablePresets = new ObservableCollection<Preset>(
            _assetManager.Presets.Select(p => new Preset
            {
                Id = p.Id,
                Name = p.Name,
                Machines = new List<string>(p.Machines),
                NavigateToPresetCommand = null,
                IsSelected = false
            })
        );

        // Reset selection
        if (AvailablePresets.Count > 0)
        {
            SelectedPreset = AvailablePresets[0];
        }

        Debug.WriteLine($"Refreshed presets list. Now contains: {string.Join(", ", AvailablePresets.Select(p => p.Name))}");
    }
}



public enum ViewState
{
    ScenarioSelection,
    AssetDetails,
    Configure,
    PresetNavigation,
    Create
}