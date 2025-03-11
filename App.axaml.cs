using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Sem2Proj.ViewModels;
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
            desktop.MainWindow = new SplashWindow(() =>
            {
                
                var mainWindow = new MainWindow()
                {
                    DataContext = new MainWindowViewModel()
                };

                mainWindow.Show();
                mainWindow.Focus();
                
                desktop.MainWindow = mainWindow;
            });
        }

        base.OnFrameworkInitializationCompleted();
        
    }
}