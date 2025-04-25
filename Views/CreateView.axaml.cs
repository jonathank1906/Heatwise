using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using Sem2Proj.ViewModels;
using Avalonia.VisualTree;
using System.Diagnostics;
using Sem2Proj.Models;
using Sem2Proj.Views;

namespace Sem2Proj.Views
{
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
}