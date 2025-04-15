using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Sem2Proj.Views;
using Sem2Proj.ViewModels;
using System.Linq;

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

            var loadingViewModel = new LoadingWindowViewModel();
            var loadingWindow = new LoadingWindow(loadingViewModel);
            
            desktop.MainWindow = loadingWindow;

            loadingWindow.Topmost = true; // Switching 'Topmost' to true, to make the loading screen popup on top
            loadingWindow.Show();
            loadingWindow.Activate();  // This does not seem to work on macOS, but it's here just to be safe
            loadingWindow.Topmost = false; // Switching 'Topmost' to false, so it's not on top when the main window appears
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
