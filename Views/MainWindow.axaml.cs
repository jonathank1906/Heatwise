using System;
using FluentAvalonia.UI.Windowing;
using Avalonia.Input;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Layout;
using Avalonia.Interactivity;
namespace Sem2Proj.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            DanfossLogo.Margin = new Thickness(15, 3, 0, 0);
            DanfossLogo.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else if (OperatingSystem.IsLinux())
        {
            DanfossLogo.Margin = new Thickness(15, 3, 0, 0);
            DanfossLogo.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else if (OperatingSystem.IsMacOS())
        {
            DanfossLogo.Margin = new Thickness(0, 3, 15, 0);
            DanfossLogo.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.GetPosition(this).Y <= TopHeader.Height)
        {
            BeginMoveDrag(e);
        }
    }
}