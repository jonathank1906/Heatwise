using System;
using FluentAvalonia.UI.Windowing;
using Avalonia.Input;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Heatwise.ViewModels;
using Heatwise.Interfaces;
namespace Heatwise.Views;

public partial class MainWindow : AppWindow
{
     private Point _dragStartPoint;
    private bool _isDragging;

    public MainWindow()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
    }

private void Popup_PointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (DataContext is MainWindowViewModel vm && vm.PopupService.PopupContent is IPopupViewModel popup && popup.IsDraggable)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetPosition(this);
            _isDragging = true;
        }
    }
}

private void Popup_PointerMoved(object? sender, PointerEventArgs e)
{
    if (_isDragging && DataContext is MainWindowViewModel vm && vm.PopupService.PopupContent is IPopupViewModel popup && popup.IsDraggable)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;

            // Use the SetPopupMargin method
            var currentMargin = vm.PopupService.PopupMargin;
            vm.PopupService.SetPopupMargin(new Thickness(
                currentMargin.Left + offset.X,
                currentMargin.Top + offset.Y,
                currentMargin.Right - offset.X,
                currentMargin.Bottom - offset.Y
            ));

            _dragStartPoint = currentPoint;
        }
    }
}
    private void Popup_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        var stackPanel = this.FindControl<StackPanel>("TopHeaderContainer");

        if (OperatingSystem.IsMacOS())
        {
            if (stackPanel == null) return;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
            stackPanel.Children.Clear();

            var danfossLogo = this.FindControl<Viewbox>("DanfossLogo");
            var homeButton = this.FindControl<Button>("HomeButton");
            var settingsButton = this.FindControl<Button>("SettingsButton");
            var helpButton = this.FindControl<Button>("HelpSectionButton");

            if (danfossLogo != null && homeButton != null && settingsButton != null && helpButton != null)
            {
                stackPanel.Children.Add(homeButton);
                stackPanel.Children.Add(settingsButton);
                stackPanel.Children.Add(helpButton);
                danfossLogo.Margin = new Thickness(10, -3, 15, 0);
                stackPanel.Children.Add(danfossLogo);
            }
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PopupService.ClosePopup();
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