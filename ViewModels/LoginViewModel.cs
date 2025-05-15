using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Views;
using System.Threading.Tasks;
using Sem2Proj.Interfaces;
using System;
using Avalonia.MicroCom;


namespace Sem2Proj.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private Action _callback;

    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string errorMessage = "";

    public LoginViewModel(Action callback)
    {
        _callback = callback;
    }

    [RelayCommand]
    private async Task AttemptLogin()
    {
        if (Username == "1" && Password == "1")
        {
            _callback();
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
            await Task.Delay(3000);
            ErrorMessage = ""; 
        }
    }
}
