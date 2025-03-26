using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
namespace Sem2Proj.Views
{
    public partial class OptimizerView : UserControl
    {
        private bool _isDragging;
        private double _startY;
        private double _currentY;
        private const double UpperLimit = -30;  // Top position (OFF)
        private const double LowerLimit = 30;  // Bottom position (ON)

        public OptimizerView()
        {
            InitializeComponent();
        }

        private void Toggle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border thumb)
            {
                _isDragging = true;
                _startY = e.GetCurrentPoint(thumb).Position.Y;
                e.Pointer.Capture(thumb); // Ensure drag events are captured
            }
        }

        private void Toggle_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDragging && sender is Border thumb)
            {
                var point = e.GetCurrentPoint(thumb);
                _currentY += point.Position.Y - _startY;
                _startY = point.Position.Y;

                // Clamp to track bounds
                _currentY = Math.Clamp(_currentY, UpperLimit, LowerLimit);

                if (thumb.RenderTransform is TranslateTransform transform)
                {
                    transform.Y = _currentY; // Immediate position update (no animation)
                }
            }
        }

        private void Toggle_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is Border thumb)
            {
                _isDragging = false;
                e.Pointer.Capture(null);

                // Improved snap logic:
                bool isOn;
                if (_currentY == 0)
                {
                    // If exactly at midpoint, snap based on last direction (hysteresis)
                    isOn = (_startY > 0); // If last drag was downward, snap to ON
                }
                else
                {
                    // Otherwise, snap based on position
                    isOn = (_currentY > 0);
                }

                _currentY = isOn ? LowerLimit : UpperLimit;

                if (thumb.RenderTransform is TranslateTransform transform)
                {
                    transform.Y = _currentY;
                }

                // Optional: Add your ON/OFF logic here
                Console.WriteLine($"Toggled {(isOn ? "ON" : "OFF")}");
            }
        }
    }
}