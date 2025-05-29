using Heatwise.ViewModels;
using Heatwise.Models;
using System.Threading.Tasks;
using System;
using Heatwise.Views;

namespace Heatwise.Services;

public class StartupService
{
    public static async Task<MainWindowViewModel> LoadApplicationAsync()
    {
        var assetManager = new AssetManager();
        var sourceDataManager = new SourceDataManager();
        var popupService = new PopupService();

        var assetManagerViewModel = await Task.Run(() => new AssetManagerViewModel(assetManager, popupService));
       var optimizerViewModel = await Task.Run(() => new OptimizerViewModel(assetManager, sourceDataManager, new ResultDataManager(), popupService));
        var sourceDataManagerViewModel = await Task.Run(() => new SourceDataManagerViewModel());

        bool showHomeScreen = sourceDataManager.GetSetting("Home_Screen_On_Startup") == "On" && sourceDataManager.GetSetting("Developer_Mode") == "Off";
        var mainVM = new MainWindowViewModel(assetManagerViewModel, optimizerViewModel, sourceDataManagerViewModel, popupService, showHomeScreen);

        return mainVM;
    }
}