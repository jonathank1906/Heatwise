using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sem2Proj.Views
{
    public partial class CalendarWindow : Window
    {
        public bool IsClosed { get; private set; }
        public IEnumerable<DateTime> SelectedDates => OptimizationCalendar.SelectedDates;
        public event EventHandler<IEnumerable<DateTime>> DatesSelected;


        public event EventHandler ResetRequested;
        public CalendarWindow()
        {
            InitializeComponent(); // ← CRITICAL!
            this.Closed += (s, e) => IsClosed = true;
        }

        public void InitializeCalendar(IEnumerable<DateTime> availableDates)
        {
            if (availableDates == null || !availableDates.Any())
                return;

            var dates = availableDates.Select(x => x.Date).Distinct().ToList();
            if (!dates.Any()) return;

            OptimizationCalendar.DisplayDateStart = dates.Min();
            OptimizationCalendar.DisplayDateEnd = dates.Max();
            OptimizationCalendar.DisplayDate = dates.First();

            // Blackout dates without data
            OptimizationCalendar.BlackoutDates.Clear();

            var allDates = new List<DateTime>();
            for (var date = dates.Min(); date <= dates.Max(); date = date.AddDays(1))
            {
                allDates.Add(date);
            }

            var datesWithoutData = allDates.Except(dates).ToList();
            if (datesWithoutData.Count == 0) return;

            DateTime? rangeStart = null;
            DateTime? rangeEnd = null;

            foreach (var date in datesWithoutData.OrderBy(d => d))
            {
                if (!rangeStart.HasValue)
                {
                    rangeStart = date;
                    rangeEnd = date;
                }
                else if (date == rangeEnd.Value.AddDays(1))
                {
                    rangeEnd = date;
                }
                else
                {
                    OptimizationCalendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
                    rangeStart = date;
                    rangeEnd = date;
                }
            }

            if (rangeStart.HasValue)
            {
                OptimizationCalendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
            }
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

        private void SetRange_Click(object sender, RoutedEventArgs e)
        {
            if (OptimizationCalendar.SelectedDates.Count > 0)
            {
                // Convert to list to prevent multiple enumeration
                var selectedDates = OptimizationCalendar.SelectedDates.ToList();

                // Raise the DatesSelected event with the chosen dates
                DatesSelected?.Invoke(this, selectedDates);
            }
            Close(); // Close the window after selection
        }
    }
}