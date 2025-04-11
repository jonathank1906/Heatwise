using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Sem2Proj.ViewModels;
using Sem2Proj.Models;

namespace Sem2Proj.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        // Hook into the "Opened" event - only called after the window is shown on screen
        this.Opened += async (_, _) => await LoadApplicationAsync();
    }

    private async Task LoadApplicationAsync()
    {
        // Give UI thread a chance to render the splash screen
        await Task.Yield();

        var assetManager = new AssetManager();
        var sourceDataManager = new SourceDataManager();

        // Load ViewModels in background threads
        var assetManagerViewModel = await Task.Run(() => new AssetManagerViewModel(assetManager));
        var optimizerViewModel = await Task.Run(() => new OptimizerViewModel(assetManager, sourceDataManager));
        var homeViewModel = await Task.Run(() => new HomeViewModel());
        var sourceDataManagerViewModel = await Task.Run(() => new SourceDataManagerViewModel());

        // Simulated loading delay
        await Task.Delay(3000);

        // Switch to UI thread to show main window
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var mainWindowViewModel = new MainWindowViewModel(
                assetManagerViewModel,
                optimizerViewModel,
                homeViewModel,
                sourceDataManagerViewModel);

            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            mainWindow.Show();

            // Optionally open HomeWindow as modal
            var homeWindow = new HomeWindow();
            homeWindow.ShowDialog(mainWindow);

            Close(); // Close the splash screen
        });
    }
}
