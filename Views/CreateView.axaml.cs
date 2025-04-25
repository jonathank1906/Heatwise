using Avalonia.Controls;
using Avalonia.Interactivity;
using Sem2Proj.ViewModels;
using System.Diagnostics;

namespace Sem2Proj.Views;

public partial class CreateView : UserControl
{
    public CreateView()
    {
        InitializeComponent();
    }

    private async void OnBrowseButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AssetManagerViewModel viewModel)
        {
            Debug.WriteLine("Export button clicked.outer");
            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                Debug.WriteLine("Export button clicked.inner");
                await viewModel.BrowseImage(this);
            }
        }
    }
}