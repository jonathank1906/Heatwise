using FluentAvalonia.UI.Windowing;
using Avalonia.Controls;

using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContext = new HomeViewModel(); 
    }
}