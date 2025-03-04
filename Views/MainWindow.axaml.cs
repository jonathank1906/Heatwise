using FluentAvalonia.UI.Windowing;
using Avalonia.Controls;
using Avalonia.Input;

using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
        this.PointerPressed += OnPointerPressed;
    }
    //TitleBar.ExtendsContentIntoTitleBar = true;
    //TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    {
        BeginMoveDrag(e);
    }
}
}