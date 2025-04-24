using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Views;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sem2Proj.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly Window _loginWindow;

    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string errorMessage = "";

    public LoginViewModel(Window loginWindow)
    {
        _loginWindow = loginWindow;
    }

    [RelayCommand]
    private async Task AttemptLogin()
    {
        if (Username == "admin" && Password == "admin") // You can replace this with actual user auth later
        {
            Debug.WriteLine("Login success.");
            OpenMainApplication();
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
            await Task.Delay(2000);
            ErrorMessage = "";
        }
    }

    private void OpenMainApplication()
    {
        var loadingWindow = new LoadingWindow(new LoadingWindowViewModel());
        loadingWindow.Show();
        _loginWindow.Close();
    }
}
