using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Sem2Proj.ViewModels
{
    public class AssetManagerViewModel : INotifyPropertyChanged
    {
        private Bitmap? _imageFromBinding;
        public Bitmap? ImageFromBinding
        {
            get => _imageFromBinding;
            private set
            {
                _imageFromBinding = value;
                OnPropertyChanged();
            }
        }

        private string _selectedImageSource;
        public string SelectedImageSource
        {
            get => _selectedImageSource;
            set
            {
                _selectedImageSource = value;
                OnPropertyChanged();
                LoadImageFromSource(value);
            }
        }

        private ListItemTemplate _selectedListItem;
        public ListItemTemplate SelectedListItem
        {
            get => _selectedListItem;
            set
            {
                _selectedListItem = value;
                OnPropertyChanged();
                UpdateSelectedImageSource();
            }
        }

        public ObservableCollection<ListItemTemplate> Items { get; set; }

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
            SelectedListItem = Items[0];
            SelectedImageSource = Items[0].ImageSource;
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


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ListItemTemplate
    {
        public string Label { get; set; }
        public string ImageSource { get; set; }
    }
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.