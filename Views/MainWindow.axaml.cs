using FluentAvalonia.UI.Windowing;
using Avalonia.Controls;

using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }

    private async void OpenDialog(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new DialogWindow();
        await dialog.ShowDialog(this); // Opens the dialog as a modal window
    }
        //TitleBar.ExtendsContentIntoTitleBar = true;
        //TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
}