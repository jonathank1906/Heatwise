using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Sem2Proj.Models;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.IO;
using Avalonia.Platform.Storage;

namespace Sem2Proj.ViewModels;

public partial class CreateViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Preset> _availablePresets = new();

    [ObservableProperty]
    private Preset? _selectedPreset;

    [ObservableProperty]
    private string _presetName = string.Empty;

    public event Action? AssetCreatedSuccessfully;
    private readonly AssetManager _assetManager;

    private readonly IStorageProvider _storageProvider;


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



    public CreateViewModel(AssetManager assetManager)
    {
        _assetManager = assetManager;

        RefreshPresetList();
    }


    [RelayCommand]
    public async Task BrowseImage(Control view)
    {
        // Get the top level from the current control
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

                // Use the project-level Assets folder
                var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
                var assetsDir = Path.Combine(projectDir, "Assets");
                Directory.CreateDirectory(assetsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                var destinationPath = Path.Combine(assetsDir, fileName);

                File.Copy(filePath, destinationPath, true);
                ImagePath = Path.Combine("Assets", fileName);

                Debug.WriteLine($"Image saved to: {destinationPath}");
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
            AssetCreatedSuccessfully?.Invoke();

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