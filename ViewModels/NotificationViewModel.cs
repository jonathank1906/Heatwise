using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Enums;

namespace Sem2Proj.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty] private string message;
    [ObservableProperty] private int checkmarkOpacity = 0;
    [ObservableProperty] private int crossOpacity = 0;
    [ObservableProperty] private int warningOpacity = 0;
    [ObservableProperty] private int informationOpacity = 0;

    public IRelayCommand DismissCommand { get; } // Might use this for dismissing the notification via a button
    public NotificationViewModel(string message, NotificationType type, Action<NotificationViewModel> dismissCallback)
    {
        Message = message;
        DismissCommand = new RelayCommand(() => dismissCallback(this));

        switch(type)
        {
            case NotificationType.Confirmation:
                CheckmarkOpacity = 1;
                break;
            case NotificationType.Error:
                CrossOpacity = 1;
                break;
            case NotificationType.Warning:
                WarningOpacity = 1;
                break;
            case NotificationType.Information:
                InformationOpacity = 1;
                break;
        }
    }
}