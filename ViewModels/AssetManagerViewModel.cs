using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using Sem2Proj.Models;
using System.Windows.Input;

namespace Sem2Proj.ViewModels
{
    public partial class AssetManagerViewModel : ObservableObject
    {
        private readonly AssetManager _assetManager = new();

        [ObservableProperty]
        private ObservableCollection<AssetModel> assets;

        [ObservableProperty]
        private Bitmap? _imageFromBinding;

        [ObservableProperty]
        private string _selectedImageSource;

        [ObservableProperty]
        private AssetModel _selectedListItem;

        public ObservableCollection<AssetModel> Items { get; set; }

        public ICommand RemovePresetItemCommand { get; }

        public AssetManagerViewModel()
        {
            //_assetManager = new AssetManager();
            Assets = new ObservableCollection<AssetModel>(_assetManager.LoadAssetsFromJson());
            SelectedListItem = Assets[0];
            SelectedImageSource = Assets[0].ImageSource;
        }

        partial void OnSelectedListItemChanged(AssetModel value)
        {
            UpdateSelectedImageSource();
        }

        partial void OnSelectedImageSourceChanged(string value)
        {
            LoadImageFromSource(value);
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
        }
    }
}