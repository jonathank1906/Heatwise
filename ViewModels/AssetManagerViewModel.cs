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
    private Preset? selectedPresetForConfiguration;

    [ObservableProperty]
    private ObservableCollection<AssetModel> assetsForSelectedPreset = new();
    // ------------------------------------------------------------------------------
    [ObservableProperty]
    private ObservableCollection<AssetModel> _machineModels = new();

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

    [ObservableProperty]
    private bool isHeatingGridVisible;


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
                assetModel.InitializePresetSelections(_assetManager.Presets);
                return assetModel;
            })
        );

        MachineModels = new ObservableCollection<AssetModel>(
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
            RefreshPresetList();
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
                    preset.MachineModels.Select(m => new AssetModel
                    {
                        Id = m.Id,
                        Name = m.Name,
                        MaxHeat = m.MaxHeat,
                        ProductionCosts = m.ProductionCosts,
                        Emissions = m.Emissions,
                        GasConsumption = m.GasConsumption,
                        OilConsumption = m.OilConsumption,
                        MaxElectricity = m.MaxElectricity,
                        ImageFromBinding = LoadImageFromSource(m.ImageSource),
                        RemoveFromPresetCommand = RemoveFromPresetCommand,
                        DeleteCommand = DeleteCommand
                    })
                )
                : new ObservableCollection<AssetModel>();
        }

        Debug.WriteLine($"Selected Scenario: {value}");
        Debug.WriteLine($"CurrentScenarioAssets Count: {CurrentScenarioAssets.Count}");
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
         Id = p.Id,
         Name = p.Name,
         Machines = new List<string>(p.Machines),
         NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name)),
         DeletePresetCommand = new RelayCommand(() => DeletePreset(p)) // Initialize DeletePresetCommand
     })
 );
 foreach (var preset in AvailablePresets)
{
    Debug.WriteLine($"[AvailablePresets] Preset: {preset.Name}, Machines: {string.Join(", ", preset.Machines)}");
}
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
            ImageSource = source.ImageSource,
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
        Debug.WriteLine($"[CreateAssetModel] Initializing preset selections for Machine: {model.Name}");
        model.InitializePresetSelections(AvailablePresets);
        return model;

    }
    [RelayCommand]
    private void RemoveFromPreset(string machineName)
    {
        if (SelectedScenario == null || SelectedScenario == "All Assets") return;

        bool success = _assetManager.RemoveMachineFromPreset(
    _assetManager.Presets.First(p => p.Name == SelectedScenario).Id,
    machineName // Pass the machine's name instead of its ID
);

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
        if (SelectedScenario == null) return;

        // Find the selected preset
        var preset = _assetManager.Presets.FirstOrDefault(p => p.Name == SelectedScenario);
        if (preset == null) return;

        // Set the selected preset for configuration
        SelectedPresetForConfiguration = preset;

        // Populate the assets specific to the selected preset
        AssetsForSelectedPreset = new ObservableCollection<AssetModel>(
            preset.MachineModels.Select(m => new AssetModel
            {
                Id = m.Id,
                Name = m.Name,
                MaxHeat = m.MaxHeat,
                ProductionCosts = m.ProductionCosts,
                Emissions = m.Emissions,
                GasConsumption = m.GasConsumption,
                OilConsumption = m.OilConsumption,
                MaxElectricity = m.MaxElectricity,
                ImageFromBinding = LoadImageFromSource(m.ImageSource),
                RemoveFromPresetCommand = RemoveFromPresetCommand,
                DeleteCommand = DeleteCommand
            })
        );

        CurrentViewState = ViewState.Configure;
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

        // Save machine parameter changes
        foreach (var machine in AllAssets)
        {
            Debug.WriteLine($"Saving machine: {machine.Name}");
            bool success = _assetManager.UpdateAsset(
                machine.Id,
                machine.Name,
                machine.MaxHeat,
                machine.MaxElectricity,
                machine.ProductionCosts,
                machine.Emissions,
                machine.GasConsumption,
                machine.OilConsumption
            );

            if (!success)
            {
                Debug.WriteLine($"Failed to save machine: {machine.Name}");
            }
        }

        // Save preset assignments
        foreach (var machine in AllAssets)
        {
            foreach (var preset in machine.PresetSelections)
            {
                if (preset.IsSelected)
                {
                    Debug.WriteLine($"Adding machine '{machine.Name}' to preset '{preset.PresetName}'");
                    _assetManager.AddMachineToPreset(
                        _assetManager.Presets.First(p => p.Name == preset.PresetName).Id,
                        machine
                    );
                }
                else
                {
                    Debug.WriteLine($"Removing machine '{machine.Name}' from preset '{preset.PresetName}'");
                   _assetManager.RemoveMachineFromPreset(
    _assetManager.Presets.First(p => p.Name == preset.PresetName).Id,
    machine.Name // Pass the machine's name
);
                }
            }
        }

        // Save toggle switch states (IsActive) and slider values (HeatProduction)
        foreach (var preset in AvailablePresets)
        {
            foreach (var machine in preset.MachineModels)
            {
                Debug.WriteLine($"Saving toggle and slider for machine: {machine.Name} in preset: {preset.Name}");
                _assetManager.UpdateMachineInPreset(
                    preset.Id,
                    machine.Name,
                    machine.MaxHeat,
                    machine.MaxElectricity,
                    machine.ProductionCosts,
                    machine.Emissions,
                    machine.GasConsumption,
                    machine.OilConsumption,
                    machine.IsActive,
                    machine.HeatProduction
                );
            }
        }

        Debug.WriteLine("=== Configuration save completed successfully ===");
        Events.Notification.Invoke("Configuration saved successfully!", NotificationType.Confirmation);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error saving configuration: {ex.Message}");
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
        Debug.WriteLine("=== RefreshPresets called ===");

        _assetManager.RefreshAssets();

        AvailablePresets = new ObservableCollection<Preset>(
           _assetManager.Presets.Select(p => new Preset
           {
               Id = p.Id,
               Name = p.Name,
               MachineModels = new ObservableCollection<AssetModel>(
                   p.MachineModels.Select(m => new AssetModel
                   {
                       Id = m.Id,
                       Name = m.Name,
                       MaxHeat = m.MaxHeat,
                       ProductionCosts = m.ProductionCosts,
                       Emissions = m.Emissions,
                       GasConsumption = m.GasConsumption,
                       OilConsumption = m.OilConsumption,
                       MaxElectricity = m.MaxElectricity,
                       ImageFromBinding = LoadImageFromSource(m.ImageSource),
                       IsActive = true, // Set IsActive to true by default
                       HeatProduction = m.MaxHeat, // Default value for heat production
                       RemoveFromPresetCommand = RemoveFromPresetCommand,
                       DeleteCommand = DeleteCommand
                   })
               )
           })
       );

        Debug.WriteLine($"Refreshed presets. Now have {AvailablePresets.Count} presets.");
        foreach (var preset in AvailablePresets)
        {
            Debug.WriteLine($"Preset: {preset.Name}, Machines: {preset.MachineModels.Count}");
            foreach (var machine in preset.MachineModels)
            {
                Debug.WriteLine($"  - Machine: {machine.Name}");
            }
        }
    }

    private bool SaveAssetChanges(AssetModel asset, int presetId)
    {
        try
        {
            Debug.WriteLine($"Saving changes for asset: {asset.Name} in preset ID: {presetId}");

            // Update the asset in the PresetMachines table
            bool success = _assetManager.UpdateMachineInPreset(
                presetId,
                asset.Name,
                asset.MaxHeat,
                asset.MaxElectricity,
                asset.ProductionCosts,
                asset.Emissions,
                asset.GasConsumption,
                asset.OilConsumption,
                asset.IsActive, // Save IsActive
                asset.HeatProduction // Save HeatProduction
            );

            if (!success)
            {
                Debug.WriteLine($"Failed to save asset: {asset.Name} in preset ID: {presetId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save {asset.Name} in preset ID: {presetId}: {ex.Message}");
            return false;
        }
    }

    private bool UpdatePresetAssignments()
    {
        bool allUpdatesSuccessful = true;

        foreach (var asset in AllAssets)
        {
            foreach (var preset in asset.AvailablePresets)
            {
                bool shouldBeInPreset = preset.IsSelected;
                bool isInPreset = _assetManager.IsMachineInPreset(preset.Id, asset.Id);

                if (shouldBeInPreset && !isInPreset)
                {
                    Debug.WriteLine($"Adding machine '{asset.Name}' to preset ID {preset.Id}");
                    if (!_assetManager.AddMachineToPreset(preset.Id, asset))
                    {
                        Debug.WriteLine($"Failed to add machine '{asset.Name}' to preset ID {preset.Id}");
                        allUpdatesSuccessful = false;
                    }
                }
                else if (!shouldBeInPreset && isInPreset)
                {
                    Debug.WriteLine($"Removing machine '{asset.Name}' from preset ID {preset.Id}");
                   if (!_assetManager.RemoveMachineFromPreset(preset.Id, asset.Name))
{
    Debug.WriteLine($"Failed to remove machine '{asset.Name}' from preset ID {preset.Id}");
    allUpdatesSuccessful = false;
}
                }
            }
        }

        return allUpdatesSuccessful;
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

            // Reset form fields
            MachineName = string.Empty;
            ImagePath = null;
            MaxHeatOutput = "0";
            MaxElectricityOutput = "0";
            ProductionCost = "0";
            Co2Emissions = "0";
            GasConsumption = "0";
            OilConsumption = "0";

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

            // Reset form field
            PresetName = string.Empty;
        }
        else
        {
            Debug.WriteLine("Failed to create new preset");
            Events.Notification.Invoke("Failed to create new preset.", Enums.NotificationType.Error);
        }
        CurrentViewState = ViewState.PresetNavigation;
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
           IsSelected = false,
           NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name)),
           DeletePresetCommand = new RelayCommand(() => DeletePreset(p)) // Initialize DeletePresetCommand
       })
   );

        // Reset selection
        if (AvailablePresets.Count > 0)
        {
            SelectedPreset = AvailablePresets[0];
        }

        Debug.WriteLine($"Refreshed presets list. Now contains: {string.Join(", ", AvailablePresets.Select(p => p.Name))}");
    }


    [RelayCommand]
    private void DeletePreset(Preset preset)
    {
        if (preset == null) return;

        bool success = _assetManager.DeletePreset(preset.Id);
        if (success)
        {
            AvailablePresets.Remove(preset);
            Events.Notification.Invoke($"Preset '{preset.Name}' deleted successfully.", NotificationType.Confirmation);
        }
        else
        {
            Events.Notification.Invoke($"Failed to delete preset '{preset.Name}'.", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void ToggleHeatingGridView()
    {
        IsHeatingGridVisible = !IsHeatingGridVisible;
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