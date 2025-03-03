using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sem2Proj.Views
{
    public partial class ScenarioManagerView : UserControl
    {
        public ScenarioManagerView()
        {
            InitializeComponent();
        }

        private async void OpenDialog(object? sender, RoutedEventArgs e)
        {
            var dialog = new DialogWindow();
            var result = await dialog.ShowDialog<bool>(GetWindow());
            // Handle the result if needed
        }

        private Window GetWindow()
        {
            return (Window)this.VisualRoot;
        }
    }
}