using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Interfaces;
using Sem2Proj.Enums;
using Avalonia.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;
using System.Diagnostics;

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
    private ICommand? _parentDeleteMachineCommand;


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




    // Create - Form properties
    [ObservableProperty]
    private string _machineName = "";

    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private string _maxHeatOutput = "";

    [ObservableProperty]
    private string _maxElectricityOutput = "";

    [ObservableProperty]
    private string _productionCost = "";

    [ObservableProperty]
    private string _co2Emissions = "";

    [ObservableProperty]
    private string _gasConsumption = "";

    [ObservableProperty]
    private string _oilConsumption = "";

    [ObservableProperty]
    private string color;

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
                IsPresetSelected = p.IsPresetSelected,
                DeletePresetCommand = DeletePresetCommand,
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

        MachineModels = new ObservableCollection<AssetModel>(
       _assetManager.Presets
           .SelectMany(p => p.MachineModels) // Flatten all machine models from all presets
           .Select(m => new AssetModel
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
               IsActive = m.IsActive,
               HeatProduction = m.HeatProduction,
               DeleteMachineCommand = DeleteMachineCommand,
               Color = m.Color
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
            if (AvailablePresets.Count > 0 && !AvailablePresets.Any(p => p.IsPresetSelected))
        {
            AvailablePresets[0].IsPresetSelected = true;
        }
        }
        else if (_availablePresets.Any(p => p.Name == destination))
        {
if (AvailablePresets.Count > 0 && !AvailablePresets.Any(p => p.IsPresetSelected))
        {
            AvailablePresets[0].IsPresetSelected = true;
        }
            CurrentViewState = ViewState.AssetDetails;
            SelectedScenario = destination;
        }
        else if (destination == "PresetNavigation")
        {
ShowScenarioSelection = true;

            CurrentViewState = ViewState.PresetNavigation;
            SelectedScenario = null;
            RefreshPresetList();
if (AvailablePresets.Count > 0 && !AvailablePresets.Any(p => p.IsPresetSelected))
        {
            AvailablePresets[0].IsPresetSelected = true;
        }
        }
        else if (destination == "Presets")
        {
if (AvailablePresets.Count > 0 && !AvailablePresets.Any(p => p.IsPresetSelected))
        {
            AvailablePresets[0].IsPresetSelected = true;
        }
            CurrentViewState = ViewState.PresetNavigation;
            SelectedScenario = null;
        }
        else
        {
            if (AvailablePresets.Count > 0 && !AvailablePresets.Any(p => p.IsPresetSelected))
        {
            AvailablePresets[0].IsPresetSelected = true;
        }
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
                    IsActive = m.IsActive,
                    HeatProduction = m.HeatProduction,
                    DeleteMachineCommand = DeleteMachineCommand,
                    Color = m.Color
                })
            )
            : new ObservableCollection<AssetModel>();


        Debug.WriteLine($"Selected Scenario: {value}");
        Debug.WriteLine($"CurrentScenarioAssets Count: {CurrentScenarioAssets.Count}");
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

        RefreshPresets();
    }

    [RelayCommand]
    private void DeleteMachine(string machineName)
    {
        if (SelectedPresetForConfiguration == null) return;

        // Find the machine in the current configuration and mark it for removal
        var machine = AssetsForSelectedPreset.FirstOrDefault(m => m.Name == machineName);
        if (machine != null)
        {
            AssetsForSelectedPreset.Remove(machine);
            Debug.WriteLine($"Machine {machineName} marked for removal from preset {SelectedPresetForConfiguration.Name}");
        }
        else
        {
            Debug.WriteLine($"Machine {machineName} not found in the current configuration.");
        }
    }
    partial void OnColorChanged(string value)
    {
        Debug.WriteLine($"Color property changed to: {value}");
    }
    [RelayCommand]
    private void Delete(int machineId)
    {
        bool success = _assetManager.DeleteMachineFromPreset(
    SelectedPresetForConfiguration?.Id ?? -1, // Use the selected preset ID
    AllAssets.FirstOrDefault(a => a.Id == machineId)?.Name ?? string.Empty // Find the machine name by ID
);

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

    public List<int> GetSelectedPresetIds()
    {
        return AvailablePresets
            .Where(p => p.IsSelected)
            .Select(p => p.Id)
            .ToList();
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
                OriginalName = m.Name,
                MaxHeat = m.MaxHeat,
                ProductionCosts = m.ProductionCosts,
                Emissions = m.Emissions,
                GasConsumption = m.GasConsumption,
                OilConsumption = m.OilConsumption,
                MaxElectricity = m.MaxElectricity,
                ImageFromBinding = LoadImageFromSource(m.ImageSource),
                IsActive = m.IsActive,
                HeatProduction = m.HeatProduction,
                DeleteMachineCommand = DeleteMachineCommand,
                Color = m.Color

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

        if (SelectedPresetForConfiguration == null)
        {
            Debug.WriteLine("Error: No preset selected for configuration.");
            Events.Notification.Invoke("No preset selected for configuration.", NotificationType.Error);
            return;
        }

        // Save or update machines in the database
        foreach (var machine in AssetsForSelectedPreset)
        {
            Debug.WriteLine($"Saving machine: {machine.Name} (OriginalName: {machine.OriginalName}) in preset: {SelectedPresetForConfiguration.Name}");

            bool success = _assetManager.UpdateMachineInPreset(
                SelectedPresetForConfiguration.Id,
                machine.OriginalName,
                machine.Name,
                machine.MaxHeat,
                machine.MaxElectricity,
                machine.ProductionCosts,
                machine.Emissions,
                machine.GasConsumption,
                machine.OilConsumption,
                machine.IsActive,
                machine.HeatProduction,
                machine.Color // Include the color property
            );
            if (!success)
            {
                Debug.WriteLine($"Failed to save machine: {machine.Name}");
                Events.Notification.Invoke($"Failed to save machine: {machine.Name}", NotificationType.Error);
            }
            else
            {
                Debug.WriteLine($"Machine '{machine.Name}' saved successfully.");
                machine.OriginalName = machine.Name; // Update OriginalName after successful save
            }
        }

        // Remove machines marked for deletion
        var machinesToRemove = SelectedPresetForConfiguration.MachineModels
            .Where(m => !AssetsForSelectedPreset.Any(am => am.Name == m.Name || am.OriginalName == m.Name))
            .ToList();

        foreach (var machine in machinesToRemove)
        {
            Debug.WriteLine($"Removing machine: {machine.Name} from preset: {SelectedPresetForConfiguration.Name}");

            bool success = _assetManager.RemoveMachineFromPreset(SelectedPresetForConfiguration.Id, machine.Name);
            if (success)
            {
                Debug.WriteLine($"Machine '{machine.Name}' removed from preset: {SelectedPresetForConfiguration.Name}");
                SelectedPresetForConfiguration.MachineModels.Remove(machine);
            }
            else
            {
                Debug.WriteLine($"Failed to remove machine '{machine.Name}' from preset: {SelectedPresetForConfiguration.Name}");
                Events.Notification.Invoke($"Failed to remove machine: {machine.Name}", NotificationType.Error);
            }
        }

        // Refresh the preset data from the database
        _assetManager.RefreshAssets();
        var updatedPreset = _assetManager.Presets.FirstOrDefault(p => p.Id == SelectedPresetForConfiguration.Id);
        if (updatedPreset != null)
        {
            SelectedPresetForConfiguration = updatedPreset;
            AssetsForSelectedPreset = new ObservableCollection<AssetModel>(
                updatedPreset.MachineModels.Select(m => new AssetModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    OriginalName = m.Name,
                    MaxHeat = m.MaxHeat,
                    ProductionCosts = m.ProductionCosts,
                    Emissions = m.Emissions,
                    GasConsumption = m.GasConsumption,
                    OilConsumption = m.OilConsumption,
                    MaxElectricity = m.MaxElectricity,
                    ImageFromBinding = LoadImageFromSource(m.ImageSource),
                    IsActive = m.IsActive,
                    HeatProduction = m.HeatProduction,
                    DeleteMachineCommand = DeleteMachineCommand,
                    Color = m.Color // Include the color property in the updated preset
                })
            );
        }


        OnSelectedScenarioChanged(SelectedScenario);
foreach (var preset in AvailablePresets)
{
    preset.IsPresetSelected = preset.Id == SelectedPresetForConfiguration.Id;
    Debug.WriteLine($"Preset: {preset.Name}, IsPresetSelected: {preset.IsPresetSelected}");
}
        //CurrentViewState = ViewState.PresetNavigation;
        NavigateTo("PresetNavigation");
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
               IsPresetSelected = p.IsPresetSelected,
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
                       IsActive = true,
                       HeatProduction = m.MaxHeat,
                       DeleteMachineCommand = DeleteMachineCommand,
                       Color = m.Color
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
            Events.Notification.Invoke("Invalid numeric input values.", NotificationType.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(MachineName))
        {
            Debug.WriteLine("Machine name cannot be empty");
            Events.Notification.Invoke("Machine name cannot be empty.", NotificationType.Error);
            return;
        }

        // Get selected preset IDs
        var selectedPresetIds = GetSelectedPresetIds();
        if (!selectedPresetIds.Any())
        {
            Debug.WriteLine("No presets selected for the machine.");
            Events.Notification.Invoke("Please select at least one preset.", NotificationType.Error);
            return;
        }

        // Create the machine for each selected preset
        foreach (var presetId in selectedPresetIds)
        {
            bool success = _assetManager.CreateNewMachine(
                MachineName,
                ImagePath ?? string.Empty,
                maxHeat,
                maxElectricity,
                productionCost,
                emissions,
                gasConsumption,
                oilConsumption,
                presetId,
                Color // Pass the color property
            );

            if (success)
            {
                Debug.WriteLine($"Successfully created new machine '{MachineName}' in PresetId {presetId}");
            }
            else
            {
                Debug.WriteLine($"Failed to create new machine '{MachineName}' in PresetId {presetId}");
                Events.Notification.Invoke($"Failed to create machine '{MachineName}' in preset.", NotificationType.Error);
            }
        }

        // Refresh the presets and reset the form
        _assetManager.RefreshAssets();
        MachineName = string.Empty;
        ImagePath = null;
        MaxHeatOutput = "0";
        MaxElectricityOutput = "0";
        ProductionCost = "0";
        Co2Emissions = "0";
        GasConsumption = "0";
        OilConsumption = "0";

        Events.Notification.Invoke("Machine created successfully!", NotificationType.Confirmation);
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
              IsPresetSelected = p.IsPresetSelected,
           DeletePresetCommand = DeletePresetCommand,
           NavigateToPresetCommand = new RelayCommand(() => NavigateTo(p.Name)),

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

    [RelayCommand]
    private void StartRenaming(Preset preset)
    {
        if (preset == null) return;

        // Set all other presets' IsRenaming to false
        foreach (var p in AvailablePresets)
        {
            p.IsRenaming = false;
        }

        // Enable renaming for the selected preset
        preset.IsRenaming = true;
    }

    [RelayCommand]
    private void FinishRenaming(Preset preset)
    {
        if (preset == null) return;

        // Disable renaming mode
        preset.IsRenaming = false;

        // Save the new name to the database

        var tempList = new ObservableCollection<Preset>(AvailablePresets);
        AvailablePresets.Clear();
        foreach (var item in tempList)
        {
            AvailablePresets.Add(item); // Re-add items to force UI update
        }
        _assetManager.UpdatePresetName(preset.Id, preset.Name);

        Events.Notification.Invoke($"Preset renamed to '{preset.Name}' successfully.", NotificationType.Confirmation);
    }

    [RelayCommand]
    private void RestoreDefaults()
    {
        _assetManager.RestoreDefaults();
        RefreshPresetList();
        Events.Notification.Invoke("Defaults restored successfully!", NotificationType.Confirmation);
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