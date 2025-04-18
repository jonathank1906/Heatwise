using System;
using FluentAvalonia.UI.Windowing;
using Avalonia.Input;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Sem2Proj.ViewModels;
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
        var stackPanel = this.FindControl<StackPanel>("TopHeaderContainer");

        if (OperatingSystem.IsMacOS())
        {
            if (stackPanel == null) return;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
            stackPanel.Children.Clear();

            var danfossLogo = this.FindControl<Image>("DanfossLogo");
            var homeButton = this.FindControl<Button>("homeButton");
            var settingsButton = this.FindControl<Button>("settingsButton");
            var testButton = this.FindControl<Button>("testButton");

            if (danfossLogo != null && homeButton != null && settingsButton != null && testButton != null)

            {
                stackPanel.Children.Add(testButton);
                stackPanel.Children.Add(homeButton);
                stackPanel.Children.Add(settingsButton);
                danfossLogo.Margin = new Thickness(0, 3, 3, 0);
                stackPanel.Children.Add(danfossLogo);
            }
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ClosePopup();
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