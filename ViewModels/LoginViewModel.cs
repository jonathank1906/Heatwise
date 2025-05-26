using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Views;
using System.Threading.Tasks;
using Heatwise.Interfaces;
using System;
using Avalonia.MicroCom;
using Avalonia.Threading;


namespace Heatwise.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    public event Action? Success;

    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string errorMessage = "";

    public LoginViewModel() {}

   [RelayCommand]
private void AttemptLogin()
{
    if (Username == "admin" && Password == "admin")
    {
        Success?.Invoke();
    }
    else
    {
        ErrorMessage = "Invalid username or password.";
        // Start a timer to clear the message after 3 seconds
        DispatcherTimer.RunOnce(() => 
        {
            ErrorMessage = "";
        }, TimeSpan.FromSeconds(3));
    }
}
}
