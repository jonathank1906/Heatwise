using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Interfaces;

namespace Sem2Proj.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty] private string message;
    public IRelayCommand DismissCommand { get; } // Might use this for dismissing the notification via a button
    public NotificationViewModel(string message, Action<NotificationViewModel> dismissCallback)
    {
        Message = message;
        DismissCommand = new RelayCommand(() => dismissCallback(this));
    }
}