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
    public event Action? Success;

    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string errorMessage = "";

    public LoginViewModel() {}

    [RelayCommand]
    private async Task AttemptLogin()
    {
        if (Username == "1" && Password == "1")
        {
            Success?.Invoke();
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
            await Task.Delay(3000);
            ErrorMessage = ""; 
        }
    }
}
