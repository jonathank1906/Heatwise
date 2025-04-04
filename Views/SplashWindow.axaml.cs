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
    }

    public async void LoadApplicationAsync()
    {
        // Ensure the splash screen is rendered before starting the loading process
        await Task.Yield();
          var assetManager = new AssetManager();
        var sourceDataManager = new SourceDataManager();
        // Perform the loading on a background thread
        var assetManagerViewModel = await Task.Run(() => new AssetManagerViewModel());
        var optimizerViewModel = await Task.Run(() => new OptimizerViewModel(assetManager, sourceDataManager));
        var homeViewModel = await Task.Run(() => new HomeViewModel());
        var sourceDataManagerViewModel = await Task.Run(() => new SourceDataManagerViewModel());

        // Simulate a delay to show the splash screen (optional)
        await Task.Delay(3000);

        // Launch the main window with the preloaded view models
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
            var homeWindow = new HomeWindow();
            mainWindow.Show();
            homeWindow.ShowDialog(mainWindow);
            Close();
        });
    }
}