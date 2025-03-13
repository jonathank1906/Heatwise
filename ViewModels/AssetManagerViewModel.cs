using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Sem2Proj.Models;
using System.Linq;
using System.Collections.Generic;

namespace Sem2Proj.ViewModels
{
    public partial class AssetManagerViewModel : ObservableObject
    {
        [ObservableProperty]
        private Bitmap? _imageFromBinding;

        [ObservableProperty]
        private string _selectedImageSource;

        [ObservableProperty]
        private ListItemTemplate _selectedListItem;

        [ObservableProperty]
        private Preset _selectedPreset;

        public ObservableCollection<ListItemTemplate> Items { get; set; }
        public ObservableCollection<Preset> Presets { get; set; }
        public ObservableCollection<ListItemTemplate> PresetItems { get; set; }

        public IRelayCommand<ListItemTemplate> RemoveCommand { get; }

        public AssetManagerViewModel()
        {
            Items = new ObservableCollection<ListItemTemplate>
            {
                new ListItemTemplate { Label = "Gas Boiler 1", ImageSource = "/Assets/GasBoiler.jpeg" },
                new ListItemTemplate { Label = "Gas Boiler 2", ImageSource = "/Assets/GasBoiler.jpeg" },
                new ListItemTemplate { Label = "Oil Boiler 1", ImageSource = "/Assets/OilBoiler.jpeg" },
                new ListItemTemplate { Label = "Gas Motor 1", ImageSource = "/Assets/GasMotor.jpg" },
                new ListItemTemplate { Label = "Heat Pump 1", ImageSource = "/Assets/HeatPump.jpg" },
                // Add more items as needed
            };
            Presets = new ObservableCollection<Preset>
            {
                new Preset { Name = "Scenario 1", Machines = new List<string> { "Gas Boiler 1", "Gas Boiler 2", "Oil Boiler 1" } },
                new Preset { Name = "Scenario 2", Machines = new List<string> { "Gas Motor 1", "Heat Pump 1" } }
            };
            PresetItems = new ObservableCollection<ListItemTemplate>();

            SelectedListItem = Items[0];
            SelectedImageSource = Items[0].ImageSource;

            RemoveCommand = new RelayCommand<ListItemTemplate>(RemoveItem);
        }

        private void RemoveItem(ListItemTemplate item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                if (SelectedListItem == item)
                {
                    SelectedListItem = Items.Count > 0 ? Items[0] : null;
                    SelectedImageSource = SelectedListItem?.ImageSource;
                }
            }
        }

        partial void OnSelectedListItemChanged(ListItemTemplate value)
        {
            UpdateSelectedImageSource();
        }

        partial void OnSelectedImageSourceChanged(string value)
        {
            LoadImageFromSource(value);
        }

        partial void OnSelectedPresetChanged(Preset value)
        {
            UpdatePresetItems();
        }

        private void UpdateSelectedImageSource()
        {
            if (SelectedListItem != null)
            {
                LoadImageFromSource(SelectedListItem.ImageSource);
            }
        }

        private void LoadImageFromSource(string imageSource)
        {
            // Ensure imageSource doesn't have a leading slash
            if (imageSource.StartsWith("/"))
            {
                imageSource = imageSource.TrimStart('/');
            }

            // Get the base directory (where the app is running)
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Construct the full path to the image inside the Assets folder
            string path = Path.Combine(basePath, imageSource);

            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    ImageFromBinding = new Bitmap(stream);
                }
            }
            else
            {
                Console.WriteLine($"Image not found: {path}");
            }
        }

        private void UpdatePresetItems()
        {
            PresetItems.Clear();
            if (SelectedPreset != null)
            {
                foreach (var machineName in SelectedPreset.Machines)
                {
                    var item = Items.FirstOrDefault(i => i.Label == machineName);
                    if (item != null)
                    {
                        PresetItems.Add(item);
                    }
                }
            }
        }
    }

    public class ListItemTemplate
    {
        public string Label { get; set; }
        public string ImageSource { get; set; }
    }
}