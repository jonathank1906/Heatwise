using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Sem2Proj.ViewModels;
using System.Linq;
using Sem2Proj.Views;

namespace Sem2Proj;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        DisableAvaloniaDataAnnotationValidation();
        
        desktop.MainWindow = new SplashWindow(() =>
        {
            // Create and show MainWindow first
            var mainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel()
            };
            
            // Create and show HomeWindow as a modal dialog
            var homeWindow = new HomeWindow();
            
            // Show main window first (non-blocking)
            mainWindow.Show();
            
            // Show home window as modal (blocking)
            homeWindow.ShowDialog(mainWindow);
            
            // Set the main window after both are shown
            desktop.MainWindow = mainWindow;
        });
    }

    base.OnFrameworkInitializationCompleted();
}

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}