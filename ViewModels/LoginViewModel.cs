using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Views;
using System.Threading.Tasks;
using Sem2Proj.Interfaces;


namespace Sem2Proj.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
     private readonly Window _loginWindow;
    private readonly AssetManagerViewModel _assetManagerViewModel;
    private readonly OptimizerViewModel _optimizerViewModel;
    private readonly SourceDataManagerViewModel _sourceDataManagerViewModel;
    private readonly IPopupService _popupService;

    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string errorMessage = "";

     public LoginViewModel(
        Window loginWindow,
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel,
        IPopupService popupService)
    {
        _loginWindow = loginWindow;
        _assetManagerViewModel = assetManagerViewModel;
        _optimizerViewModel = optimizerViewModel;
        _sourceDataManagerViewModel = sourceDataManagerViewModel;
        _popupService = popupService;
    }

    [RelayCommand]
    private async Task AttemptLogin()
    {
        if (Username == "1" && Password == "1")
        {
            OpenMainApplication();
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
            await Task.Delay(3000);
            ErrorMessage = ""; 
        }
    }

    private void OpenMainApplication()
    {
        var mainWindow = new MainWindow
    {
        DataContext = new MainWindowViewModel(
            _assetManagerViewModel,
            _optimizerViewModel,
            _sourceDataManagerViewModel,
            _popupService)
    };
    mainWindow.Show();
    mainWindow.Activate();
    _loginWindow.Close();
    }
}
