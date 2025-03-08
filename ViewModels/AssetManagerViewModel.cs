using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

namespace Sem2Proj.ViewModels
{
    public class AssetManagerViewModel : INotifyPropertyChanged
    {
        private string _selectedImageSource;
        public string SelectedImageSource
        {
            get => _selectedImageSource;
            set
            {
                _selectedImageSource = value;
                OnPropertyChanged();
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
                new ListItemTemplate { Label = "Tab 1", ImageSource = "/Assets/GasBoiler1.png" },
                new ListItemTemplate { Label = "Tab 2", ImageSource = "/Assets/OilBoiler1.png" },
                // Add more items as needed
            };
            SelectedListItem = Items[0];
            SelectedImageSource = Items[0].ImageSource;
        }

        private void UpdateSelectedImageSource()
        {
            if (SelectedListItem != null)
            {
                SelectedImageSource = SelectedListItem.ImageSource;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
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