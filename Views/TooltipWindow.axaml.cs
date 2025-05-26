// TooltipWindow.xaml.cs
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace Heatwise.Views
{
    public partial class TooltipWindow : Window
    {
        public event EventHandler? WindowClosed;
        public bool IsClosed { get; private set; }

        public TooltipWindow()
        {
            InitializeComponent();
            this.Closed += (s, e) =>
            {
                IsClosed = true;
                WindowClosed?.Invoke(this, EventArgs.Empty);
            };
        }

        private void WindowDragMove(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void UpdateContent(string text)
        {
            var textBlock = this.FindControl<TextBlock>("TooltipText");
            textBlock!.Text = text;
        }

        public void MinimizeWithMainWindow()
        {
            if (WindowState != WindowState.Minimized)
            {
                WindowState = WindowState.Minimized;
            }
        }

        public void RestoreWithMainWindow()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
        }
    }
}