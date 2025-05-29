using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Heatwise.Events;
using Heatwise.Enums;

namespace Heatwise.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public IPopupService PopupService { get; }

    [ObservableProperty]
    private ObservableCollection<NotificationViewModel> notifications = [];

    // ViewModels
    public AssetManagerViewModel AssetManagerViewModel { get; }
    public OptimizerViewModel OptimizerViewModel { get; }
    public SourceDataManagerViewModel SourceDataManagerViewModel { get; }

    public MainWindowViewModel(
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel,
        IPopupService popupService,
        bool showHomeScreen)
    {
        AssetManagerViewModel = assetManagerViewModel;
        OptimizerViewModel = optimizerViewModel;
        SourceDataManagerViewModel = sourceDataManagerViewModel;
        PopupService = popupService;

        if (showHomeScreen) ShowHome();

        Notification.OnNewNotification += ShowNotification;
    }

    [RelayCommand]
    public void ShowHome() => PopupService.ShowPopup<HomeViewModel>();

    [RelayCommand]
    public void ShowSettings() => PopupService.ShowPopup<SettingsViewModel>();

    [RelayCommand]
    public void ShowAMHelp() => PopupService.ShowPopup<AMHelpViewModel>();

    [RelayCommand]
    public void ShowOPTHelp() => PopupService.ShowPopup<OPTHelpViewModel>();

    public void ShowNotification(string message, NotificationType type)
    {
        var notification = new NotificationViewModel(message, type, RemoveNotification);
        Notifications.Add(notification);

        Task.Delay(5000).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() => RemoveNotification(notification));
        });
    }

    private void RemoveNotification(NotificationViewModel vm)
    {
        Notifications.Remove(vm);
    }
}