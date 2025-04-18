using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Models;
using Sem2Proj.Interfaces;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Sem2Proj.Events;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private bool isPopupVisible = false;
    [ObservableProperty] private object? popupContent = null;
    [ObservableProperty] private double popupOpacity = 0;
    [ObservableProperty] private ObservableCollection<NotificationViewModel> notifications = [];

    // ViewModels
    public AssetManagerViewModel AssetManagerViewModel { get; }
    public OptimizerViewModel OptimizerViewModel { get; }
    public SourceDataManagerViewModel SourceDataManagerViewModel { get; }

    public MainWindowViewModel(
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel)
    {
        AssetManagerViewModel = assetManagerViewModel;
        OptimizerViewModel = optimizerViewModel;
        SourceDataManagerViewModel = sourceDataManagerViewModel;

        // Show the home screen on startup
        PopupOpacity = 1; // Set the opacity straight to 1 to skip the animation
        ShowHome();

        // Subscribe to the event to show notifications
        Notification.OnNewNotification += ShowNotification;

        // Show a test notification on startup
        ShowNotification("Welcome to the app! This is a notification that will disappear after 3 seconds.");
    }

    // A quite fancy method to show a popup
    // It uses generics to allow any ViewModel to be shown in the popup
    // It also sets the close action for the ViewModel
    // Could have used a more generic approach, but this is cooler
    private void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new()
    {
        var viewModel = new TViewModel();
        viewModel.SetCloseAction(ClosePopup);
        PopupContent = viewModel;
        IsPopupVisible = true;

        if (PopupOpacity == 1) return;

        // Animate the popup opacity
        // Working with the Avalonia animations is a pain so this will do for now
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            for (double i = 0; i <= 1; i += 0.02)
            {
                PopupOpacity = i;
                await Task.Delay(1);
            }
            PopupOpacity = 1;
        });
    }

    public void ClosePopup()
    {
        // Animate the popup opacity
        // Working with the Avalonia animations is a pain so this will do for now
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            for (double i = 1; i > 0; i -= 0.02)
            {
                PopupOpacity = i;
                await Task.Delay(1);
            }

            PopupOpacity = 0;
            IsPopupVisible = false;
            PopupContent = null;
        });
    }

    [RelayCommand]
    public void ShowHome()
    {
        ShowPopup<HomeViewModel>();
    }

    [RelayCommand]
    public void ShowSettings()
    {
        ShowPopup<SettingsViewModel>();
    }

    public void ShowNotification(string message)
    {
        var notification = new NotificationViewModel(message, RemoveNotification);
        Notifications.Add(notification);

        // Auto remove after 3 seconds
        Task.Delay(3000).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() => RemoveNotification(notification));
        });
    }

    private void RemoveNotification(NotificationViewModel vm)
    {
        Notifications.Remove(vm);
    }



    // Testing
    private int testCounter = 0;

    [RelayCommand]
    public void ShowTest()
    {
        ShowNotification($"This is a test notification #{++testCounter}");
    }
}