using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Sem2Proj.Views;
using Sem2Proj.ViewModels;
using System.Linq;
using Avalonia.Styling;  // For ThemeVariant
using Avalonia.Controls; // For FindResource
using System.Diagnostics;
using System;
using Sem2Proj.Services;
using System.Threading.Tasks;

namespace Sem2Proj;

public partial class App : Application
{
    public static event Action? ThemeChanged;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Fetch settings
            SettingsViewModel settings = new();

            // Apply the initial theme
            UpdateTheme(settings.Light_Mode_On_Toggle);

            // Load the application separately
            MainWindowViewModel mainVM = await StartupService.LoadApplicationAsync();
            MainWindow mainWindow = new() { DataContext = mainVM };

            // Starting the application in Developer Mode or in Regular Mode
            if (settings.Developer_Mode_On_Toggle)
            {
                mainWindow.Show();
            }
            else
            {
                LoadingWindowViewModel loadingViewModel = new();
                LoadingWindow loadingWindow = new() { DataContext = loadingViewModel };
                LoginViewModel loginViewModel = new();
                LoginView loginView = new() { DataContext = loginViewModel };

                loadingWindow.Topmost = true; // Switching 'Topmost' to true, to make the loading screen popup on top
                loadingWindow.Show();
                loadingWindow.Activate();  // This does not seem to work on macOS, but it's here just to be safe
                loadingWindow.Topmost = false; // Switching 'Topmost' to false, so it's not on top when the main window appears
                loadingViewModel.Finished += () =>
                {
                    desktop.MainWindow = loginView;
                    loginView.Show();
                    loginView.Activate();
                    loadingWindow.Close();
                };
                await loadingViewModel.Start();

                loginViewModel.Success += () =>
                {

                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    mainWindow.Activate();
                    loginView.Close();
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void UpdateTheme(bool Light_Mode_On_Toggle)
    {
        if (Application.Current == null) return;

        // Get the merged dictionaries
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        var themeColors = mergedDictionaries.FirstOrDefault() as ResourceDictionary;

        if (themeColors == null)
        {
            Debug.WriteLine("ThemeColors dictionary not found!");
            return;
        }

        // Debug: Print all available keys and their values
        Debug.WriteLine("Available resource keys and values:");
        foreach (var key in themeColors.Keys)
        {
            var value = themeColors[key];
            Debug.WriteLine($"{key}: {value}");
        }

        try
        {
            if (!Light_Mode_On_Toggle)
            {
                Current.Resources["BackgroundColor"] = themeColors["LightBackgroundColor"];
                Current.Resources["ForegroundColor"] = themeColors["LightForegroundColor"];
                Current.Resources["AccentColor"] = themeColors["LightAccentColor"];
                Current.Resources["TextColor"] = themeColors["LightTextColor"];
                Current.Resources["SecondaryBackground"] = themeColors["LightSecondaryBackground"];
                Current.Resources["BorderColor"] = themeColors["LightBorderColor"];
                Current.Resources["IconColor"] = themeColors["LightIconColor"];
                Current.Resources["GradientStartColor"] = themeColors["LightGradientStartColor"];
                Current.Resources["GradientEndColor"] = themeColors["LightGradientEndColor"];
            }
            else
            {
                Current.Resources["BackgroundColor"] = themeColors["DarkBackgroundColor"];
                Current.Resources["ForegroundColor"] = themeColors["DarkForegroundColor"];
                Current.Resources["AccentColor"] = themeColors["DarkAccentColor"];
                Current.Resources["TextColor"] = themeColors["DarkTextColor"];
                Current.Resources["SecondaryBackground"] = themeColors["DarkSecondaryBackground"];
                Current.Resources["BorderColor"] = themeColors["DarkBorderColor"];
                Current.Resources["IconColor"] = themeColors["DarkIconColor"];
                Current.Resources["GradientStartColor"] = themeColors["DarkGradientStartColor"];
                Current.Resources["GradientEndColor"] = themeColors["DarkGradientEndColor"];

            }
            ThemeChanged?.Invoke();
            Debug.WriteLine($"Theme updated to {(Light_Mode_On_Toggle ? "Light" : "Dark")} mode");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating theme: {ex.Message}");
        }

        // Force UI update
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.InvalidateVisual();
            }
        }
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