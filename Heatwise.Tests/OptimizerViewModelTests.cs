using Xunit;
using Moq;
using Heatwise.ViewModels;
using Heatwise.Models;
using Heatwise.Services;
using Heatwise.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Heatwise.Enums;

namespace Heatwise.Tests
{
    public class OptimizerViewModelTests
    {
        private Mock<AssetManager> _mockAssetManager;
        private Mock<SourceDataManager> _mockSourceDataManager;
        private Mock<ResultDataManager> _mockResultDataManager;
        private Mock<IPopupService> _mockPopupService;
        private OptimizerViewModel _viewModel;

        public OptimizerViewModelTests()
        {
            _mockAssetManager = new Mock<AssetManager>();
            _mockSourceDataManager = new Mock<SourceDataManager>();
            _mockResultDataManager = new Mock<ResultDataManager>();
            _mockPopupService = new Mock<IPopupService>();
            _viewModel = new OptimizerViewModel(_mockAssetManager.Object, _mockSourceDataManager.Object, _mockResultDataManager.Object, _mockPopupService.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Assert
            Assert.NotNull(_viewModel);
            Assert.NotNull(_viewModel.AssetManager);
            Assert.False(_viewModel.HasOptimized);
            Assert.Null(_viewModel.OptimizationResults);
            Assert.Null(_viewModel.HeatDemandData);
        }

        [Fact]
        public void Constructor_WithNullAssetManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OptimizerViewModel(null, _mockSourceDataManager.Object, _mockResultDataManager.Object, _mockPopupService.Object));
        }

        [Fact]
        public void Constructor_WithNullSourceDataManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OptimizerViewModel(_mockAssetManager.Object, null, _mockResultDataManager.Object, _mockPopupService.Object));
        }

        [Fact]
        public void Constructor_WithNullResultDataManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OptimizerViewModel(_mockAssetManager.Object, _mockSourceDataManager.Object, null, _mockPopupService.Object));
        }

        [Fact]
        public void SwitchGraph_WithHeatProductionType_ShouldUpdateView()
        {
            // Arrange
            var optimizationResults = new List<HeatProductionResult>();
            var heatDemandData = new List<(DateTime timestamp, double value)>();
            bool plotWasCalled = false;

            _viewModel.OptimizationResults = optimizationResults;
            _viewModel.HeatDemandData = heatDemandData;
            _viewModel.HasOptimized = true;
            _viewModel.PlotOptimizationResults = (results, demand) => plotWasCalled = true;

            // Act
            _viewModel.SwitchGraph(GraphType.HeatProduction);

            // Assert
            Assert.True(plotWasCalled);
        }

        [Fact]
        public void SwitchGraph_WithNoOptimizationResults_ShouldNotUpdateView()
        {
            // Arrange
            bool plotWasCalled = false;
            _viewModel.PlotOptimizationResults = (results, demand) => plotWasCalled = true;

            // Act
            _viewModel.SwitchGraph(GraphType.HeatProduction);

            // Assert
            Assert.False(plotWasCalled);
        }

        [Fact]
        public void DummyTest_OptimizerViewModel()
        {
            Assert.True(true);
        }
    }
} 