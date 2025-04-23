using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Linq;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Sem2Proj.Interfaces;
using Sem2Proj.Services;
using Sem2Proj.Enums;
using Sem2Proj.Events;


namespace Sem2Proj.ViewModels;


public partial class AssetManagerViewModel : ObservableObject
{
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
        SelectedScenario = "All Assets";

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
            ShowScenarioSelection = true;
            ShowAssetDetails = false;
            // Keep SelectedScenario as is (don't set to null)
            return;
        }

        if (destination == "All Assets" || AvailableScenarios.Contains(destination))
        {
            ShowScenarioSelection = false;
            ShowAssetDetails = true;
            SelectedScenario = destination == "All Assets" ? "All Assets" : destination;
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
                    ImageFromBinding = LoadImageFromSource(a.ImageSource)
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
                            ImageFromBinding = LoadImageFromSource(a.ImageSource)
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
        var settingsViewModel = new SettingsViewModel(_assetManager, _popupService);

        settingsViewModel.AssetCreatedSuccessfully += () =>
        {
            // Refresh the data
            _assetManager.RefreshAssets();

            // Force UI update by reassigning the collection
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
                    ImageFromBinding = LoadImageFromSource(a.ImageSource)
                })
            );

            Debug.WriteLine("New asset created and view refreshed");
            Events.Notification.Invoke("New asset created successfully!", NotificationType.Confirmation);
        };

        _popupService.ShowPopup(settingsViewModel);
    }
}