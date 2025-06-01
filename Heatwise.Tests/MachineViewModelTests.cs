using Xunit;
using Moq;
using Heatwise.ViewModels;
using Heatwise.Models;
using Heatwise.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Heatwise.Interfaces;

namespace Heatwise.Tests
{
    public class MachineViewModelTests
    {
        private Mock<AssetManager> _mockAssetManager;
 
        private AssetManagerViewModel _viewModel;

        public MachineViewModelTests()
        {
            _mockAssetManager = new Mock<AssetManager>();
       
            _viewModel = new AssetManagerViewModel(_mockAssetManager.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Assert
            Assert.NotNull(_viewModel);
            Assert.NotNull(_viewModel.MachineModels);
            Assert.NotNull(_viewModel.AvailablePresets);
            Assert.NotNull(_viewModel.AvailableScenarios);
            Assert.Equal(ViewState.PresetNavigation, _viewModel.CurrentViewState);
        }

        [Fact]
        public void Constructor_WithNullAssetManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AssetManagerViewModel(null));
        }

        [Fact]
        public void NavigateTo_WithAllAssets_ShouldUpdateViewState()
        {
            // Act
            _viewModel.NavigateTo("All Assets");

            // Assert
            Assert.Equal(ViewState.AssetDetails, _viewModel.CurrentViewState);
            Assert.Equal("All Assets", _viewModel.SelectedScenario);
        }

        [Fact]
        public void NavigateTo_WithPresetNavigation_ShouldUpdateViewState()
        {
            // Act
            _viewModel.NavigateTo("PresetNavigation");

            // Assert
            Assert.Equal(ViewState.PresetNavigation, _viewModel.CurrentViewState);
            Assert.Null(_viewModel.SelectedScenario);
        }

        [Fact]
        public void NavigateTo_WithPresets_ShouldUpdateViewState()
        {
            // Act
            _viewModel.NavigateTo("Presets");

            // Assert
            Assert.Equal(ViewState.PresetNavigation, _viewModel.CurrentViewState);
            Assert.Null(_viewModel.SelectedScenario);
        }

        [Fact]
        public void ShowConfiguration_ShouldUpdateViewState()
        {
            // Arrange
            var assetManager = new AssetManager();
            var preset = new Preset { Name = "All Assets", Id = 1 };
            assetManager.Presets.Add(preset);
            _viewModel = new AssetManagerViewModel(assetManager);
            _viewModel.NavigateTo("All Assets"); // Set a scenario first

            // Act
            _viewModel.ShowConfiguration();

            // Assert
            Assert.Equal(ViewState.Configure, _viewModel.CurrentViewState);
            Assert.True(_viewModel.IsConfiguring);
        }

        [Fact]
        public void CancelConfiguration_ShouldUpdateViewState()
        {
            // Arrange
            _viewModel.NavigateTo("All Assets"); // Set a scenario first
            _viewModel.NavigateTo("Configure");

            // Act
            _viewModel.NavigateTo("PresetNavigation");

            // Assert
            Assert.Equal(ViewState.PresetNavigation, _viewModel.CurrentViewState);
            Assert.False(_viewModel.IsConfiguring);
        }
    }
} 