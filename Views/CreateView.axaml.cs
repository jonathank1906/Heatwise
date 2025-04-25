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
            Debug.WriteLine("OnBrowseButtonClick invoked.");
            var viewModel = new CreateViewModel(new AssetManager());

            // Get the TopLevel for the current view
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                Debug.WriteLine("TopLevel is not null.");
                await viewModel.BrowseImage(this);
            }
            else
            {
                Debug.WriteLine("TopLevel is null.");
            }

            // if (DataContext is CreateViewModel viewModel)
            // {
            //     Debug.WriteLine("DataContext is CreateViewModel.");
            //     var topLevel = TopLevel.GetTopLevel(this);
            //     if (topLevel != null)
            //     {
            //         Debug.WriteLine("TopLevel is not null.");
            //         await viewModel.BrowseImage(this);
            //     }
            //     else
            //     {
            //         Debug.WriteLine("TopLevel is null.");
            //     }
            // }
            // else
            // {
            //     Debug.WriteLine("DataContext is not CreateViewModel.");
            // }
        }

        private async void OnCreateMachineClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnCreateMachineClick invoked.");

            if (DataContext is CreateViewModel viewModel)
            {
                // Get the TopLevel for the current view
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    Debug.WriteLine("DataContext is CreateViewModel.");
                    await viewModel.CreateMachine(this);
                }
                else
                {
                    Debug.WriteLine("TopLevel is null.");
                }
            }
            else
            {
                Debug.WriteLine("DataContext is not CreateViewModel.");
            }
        }
    }
}