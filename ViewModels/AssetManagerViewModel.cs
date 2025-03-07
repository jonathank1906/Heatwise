using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sem2Proj.ViewModels
{
    public partial class AssetManagerViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isPaneOpen;

        [RelayCommand]
        private void TriggerPane()
        {
            IsPaneOpen = !IsPaneOpen;
        }

        [ObservableProperty]
        private ListItemTemplate? selectedListItem;

        public ObservableCollection<ListItemTemplate> Items { get; } = new ObservableCollection<ListItemTemplate>
        {
            new ListItemTemplate("GasBoiler1", "Gas Boiler 1"),
            new ListItemTemplate("GasBoiler2", "Gas Boiler 2"),
            new ListItemTemplate("OilBoiler1", "Oil Boiler 1"),
            new ListItemTemplate("GasMotor1", "Gas Motor 1"),
            new ListItemTemplate("HeatPump1", "Heat Pump 1")
        };
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(string iconKey, string label)
        {
            Label = label;
            ListItemIcon = LoadIcon(iconKey);
        }

        public string Label { get; }
        public StreamGeometry ListItemIcon { get; }

        private StreamGeometry LoadIcon(string iconKey)
        {
            // Replace this with your actual icon loading logic
            return new StreamGeometry();
        }
    }
}