using FluentAvalonia.UI.Windowing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class AssetManagerView : UserControl
{
    public AssetManagerView()
    {
        InitializeComponent();
        DataContext = new AssetManagerViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}