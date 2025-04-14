using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Linq;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;

namespace Sem2Proj.ViewModels;

public partial class LoadingWindowViewModel : ViewModelBase
{
    [ObservableProperty] private int progress;
    [ObservableProperty] private string loadingText = "Starting...";
    [ObservableProperty] private SystemDecorations systemDecorations = SystemDecorations.None;
    [ObservableProperty] private CornerRadius cornerRadius = new(15);
    [ObservableProperty] private Color gradientColor1 = Color.Parse("#E22D2A");
    [ObservableProperty] private Color gradientColor2 = Color.Parse("#2E1F20");
    private List<(string message, int steps, int delay)> messages =
    [
    ("Starting application...", 10, 10),

    ("Initializing AppData...", 15, 20),
    ("Loading configuration files...", 20, 20),
    ("Applying user settings...", 15, 20),

    ("Preparing interface...", 10, 5),
    ("Finalizing startup...", 10, 5),
    ("Almost there...", 5, 5),
    ("Finishing up...", 15, 5)
    ];

    public LoadingWindowViewModel()
    {
        // macOS specific window configuration
        if (OperatingSystem.IsMacOS())
        {
            SystemDecorations = SystemDecorations.BorderOnly;
            CornerRadius = new(0);
            GradientColor2 = Color.Parse("#1F95221E");
            GradientColor1 = Color.Parse("#EFE22D2A");
        }
    }


    public async Task LoadApplicationAsync(Action<MainWindowViewModel> onMainWindowReady)
    {
        await Task.Yield();

        var assetManager = new AssetManager();
        var sourceDataManager = new SourceDataManager();

        var assetManagerViewModel = await Task.Run(() => new AssetManagerViewModel(assetManager));
        var optimizerViewModel = await Task.Run(() => new OptimizerViewModel(assetManager, sourceDataManager, new ResultDataManager()));
        var sourceDataManagerViewModel = await Task.Run(() => new SourceDataManagerViewModel());

        foreach (var (message, steps, delay) in messages)
        {
            LoadingText = message;
            for (int i = 0; i < steps; i++)
            {
                Progress++;
                await Task.Delay(delay);
            }
        }

        await Task.Delay(50);

        var mainVM = new MainWindowViewModel(assetManagerViewModel, optimizerViewModel, sourceDataManagerViewModel);
        onMainWindowReady?.Invoke(mainVM);
    }
}