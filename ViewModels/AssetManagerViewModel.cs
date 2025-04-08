using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Linq;
using System.Diagnostics;

namespace Sem2Proj.ViewModels;

public partial class AssetManagerViewModel : ObservableObject
{
    private readonly AssetManager _assetManager;

    [ObservableProperty]
    private ObservableCollection<AssetModel> _displayedAssets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedAssets))]
    private string? _selectedScenario;

    [ObservableProperty]
    private AssetModel? _selectedAsset;

    [ObservableProperty]
    private Bitmap? _imageFromBinding;

    [ObservableProperty]
    private Bitmap? _gridImageFromBinding;

    public ObservableCollection<string> AvailableScenarios { get; }

    public HeatingGrid? GridInfo => _assetManager.GridInfo;

    public AssetManagerViewModel(AssetManager assetManager)
    {
        _assetManager = assetManager;

        // Initialize scenarios list
        AvailableScenarios = new ObservableCollection<string>(
            new[] { "All Assets" }
                .Concat(_assetManager.Presets.Select(p => p.Name))
        );

        // Set default state
        SelectedScenario = "All Assets";
        _displayedAssets = new ObservableCollection<AssetModel>(_assetManager.AllAssets);

        if (_displayedAssets.Count > 0)
        {
            SelectedAsset = _displayedAssets[0];
        }

        // Load initial grid image if available
        if (GridInfo?.ImageSource != null)
        {
            LoadGridImageFromSource(GridInfo.ImageSource);
        }

        Debug.WriteLine($"ViewModel initialized with {_displayedAssets.Count} assets");
    }

    private void UpdateDisplayedAssets()
    {
        Debug.WriteLine($"Changing scenario to: {SelectedScenario}");

        if (SelectedScenario == "All Assets")
        {
            DisplayedAssets = new ObservableCollection<AssetModel>(_assetManager.AllAssets);
        }
        else
        {
            var preset = _assetManager.Presets.FirstOrDefault(p => p.Name == SelectedScenario);
            if (preset != null)
            {
                var scenarioAssets = _assetManager.AllAssets
                    .Where(a => preset.Machines.Contains(a.Name))
                    .ToList();

                DisplayedAssets = new ObservableCollection<AssetModel>(scenarioAssets);
                Debug.WriteLine($"Loaded {scenarioAssets.Count} assets for scenario {SelectedScenario}");
            }
        }

        // Maintain selection if possible
        SelectedAsset = DisplayedAssets.Contains(SelectedAsset)
            ? SelectedAsset
            : DisplayedAssets.FirstOrDefault();

        Debug.WriteLine($"Now displaying {DisplayedAssets.Count} assets");
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
        UpdateDisplayedAssets();
    }

    private void LoadImageFromSource(string imageSource)
    {
        try
        {
            if (string.IsNullOrEmpty(imageSource))
            {
                ImageFromBinding = null;
                return;
            }

            var normalizedPath = imageSource.TrimStart('/', '\\');
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, normalizedPath);

            if (File.Exists(fullPath))
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    ImageFromBinding = new Bitmap(stream);
                }
            }
            else
            {
                Debug.WriteLine($"Image not found at: {fullPath}");
                ImageFromBinding = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading image: {ex.Message}");
            ImageFromBinding = null;
        }
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
}