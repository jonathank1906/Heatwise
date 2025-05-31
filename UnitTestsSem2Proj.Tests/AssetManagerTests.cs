using Xunit;
using Sem2Proj.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Sem2Proj.Tests
{
    public class AssetManagerTests
    {
        private readonly AssetManager _assetManager;

        public AssetManagerTests()
        {
            _assetManager = new AssetManager();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithData()
        {
            // Assert
            Assert.NotNull(_assetManager.Presets);
            Assert.NotNull(_assetManager.CurrentAssets);
            Assert.NotNull(_assetManager.GridInfo);
            Assert.True(_assetManager.Presets.Count > 0, "Should have at least one preset");
            Assert.True(_assetManager.CurrentAssets.Count > 0, "Should have at least one asset");
        }

        [Fact]
        public void SetScenario_WithValidIndex_ShouldUpdateCurrentAssets()
        {
            // Arrange
            var initialAssetCount = _assetManager.CurrentAssets.Count;
            var firstPreset = _assetManager.Presets[0];
            var expectedAssetCount = firstPreset.MachineModels.Count;

            // Act
            var result = _assetManager.SetScenario(0);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedAssetCount, _assetManager.CurrentAssets.Count);
            Assert.Equal(0, _assetManager.SelectedScenarioIndex);
            Assert.Equal(firstPreset.Name, _assetManager.CurrentScenarioName);
        }

        [Fact]
        public void SetScenario_WithInvalidIndex_ShouldReturnFalse()
        {
            // Act
            var result = _assetManager.SetScenario(-1);
            var result2 = _assetManager.SetScenario(_assetManager.Presets.Count + 1);

            // Assert
            Assert.False(result);
            Assert.False(result2);
        }

        [Fact]
        public void SetScenario_WithValidName_ShouldUpdateCurrentAssets()
        {
            // Arrange
            var firstPreset = _assetManager.Presets[0];
            var expectedAssetCount = firstPreset.MachineModels.Count;

            // Act
            var result = _assetManager.SetScenario(firstPreset.Name);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedAssetCount, _assetManager.CurrentAssets.Count);
            Assert.Equal(firstPreset.Name, _assetManager.CurrentScenarioName);
        }

        [Fact]
        public void CreateNewMachine_WithValidData_ShouldAddToDatabase()
        {
            // Arrange
            var presetId = _assetManager.Presets[0].Id;
            var initialAssetCount = _assetManager.CurrentAssets.Count;

            // Act
            var result = _assetManager.CreateNewMachine(
                "Test Machine",
                "test.png",
                100.0,  // maxHeat
                50.0,   // maxElectricity
                75.0,   // productionCost
                25.0,   // emissions
                30.0,   // gasConsumption
                20.0,   // oilConsumption
                presetId,
                "#FF0000" // color
            );

            // Assert
            Assert.True(result);
            _assetManager.RefreshAssets();
            Assert.True(_assetManager.CurrentAssets.Count >= initialAssetCount);
            Assert.Contains(_assetManager.CurrentAssets, a => a.Name == "Test Machine");

            // Cleanup - remove the test machine
            var testMachine = _assetManager.CurrentAssets.First(a => a.Name == "Test Machine");
            _assetManager.RemoveMachineFromPreset(testMachine.Id);
        }

        [Fact]
        public void UpdateMachine_WithValidData_ShouldUpdateInDatabase()
        {
            // Arrange
            var firstMachine = _assetManager.CurrentAssets[0];
            var originalName = firstMachine.Name;
            var newName = "Updated Machine";
            var newMaxHeat = 150.0;

            // Act
            var result = _assetManager.UpdateMachineInPreset(
                firstMachine.Id,
                newName,
                newMaxHeat,
                firstMachine.MaxElectricity,
                firstMachine.ProductionCosts,
                firstMachine.Emissions,
                firstMachine.GasConsumption,
                firstMachine.OilConsumption,
                firstMachine.IsActive,
                firstMachine.HeatProduction,
                firstMachine.Color
            );

            // Assert
            Assert.True(result);
            _assetManager.RefreshAssets();
            var updatedMachine = _assetManager.CurrentAssets.FirstOrDefault(a => a.Id == firstMachine.Id);
            Assert.NotNull(updatedMachine);
            Assert.Equal(newName, updatedMachine.Name);
            Assert.Equal(newMaxHeat, updatedMachine.MaxHeat);

            // Cleanup - restore original name
            _assetManager.UpdateMachineInPreset(
                firstMachine.Id,
                originalName,
                firstMachine.MaxHeat,
                firstMachine.MaxElectricity,
                firstMachine.ProductionCosts,
                firstMachine.Emissions,
                firstMachine.GasConsumption,
                firstMachine.OilConsumption,
                firstMachine.IsActive,
                firstMachine.HeatProduction,
                firstMachine.Color
            );
        }

        [Fact]
        public void CreateNewPreset_WithValidName_ShouldAddToDatabase()
        {
            // Arrange
            var initialPresetCount = _assetManager.Presets.Count;
            var newPresetName = "Test Preset";

            // Act
            var result = _assetManager.CreateNewPreset(newPresetName);

            // Assert
            Assert.True(result);
            _assetManager.RefreshAssets();
            Assert.Equal(initialPresetCount + 1, _assetManager.Presets.Count);
            Assert.Contains(_assetManager.Presets, p => p.Name == newPresetName);

            // Cleanup - remove the test preset
            var testPreset = _assetManager.Presets.First(p => p.Name == newPresetName);
            _assetManager.DeletePreset(testPreset.Id);
        }

        [Fact]
        public void DeletePreset_WithValidId_ShouldRemoveFromDatabase()
        {
            // Arrange
            var newPresetName = "Preset To Delete";
            _assetManager.CreateNewPreset(newPresetName);
            _assetManager.RefreshAssets();
            var presetToDelete = _assetManager.Presets.First(p => p.Name == newPresetName);
            var initialPresetCount = _assetManager.Presets.Count;

            // Act
            var result = _assetManager.DeletePreset(presetToDelete.Id);

            // Assert
            Assert.True(result);
            _assetManager.RefreshAssets();
            Assert.Equal(initialPresetCount - 1, _assetManager.Presets.Count);
            Assert.DoesNotContain(_assetManager.Presets, p => p.Id == presetToDelete.Id);
        }

        [Fact]
        public void AssetModel_Calculations_ShouldBeCorrect()
        {
            // Arrange
            var asset = _assetManager.CurrentAssets[0];

            // Act & Assert
            Assert.Equal(asset.ProductionCosts, asset.CostPerMW);
            Assert.Equal(asset.MaxHeat > 0 ? asset.Emissions / asset.MaxHeat : 0, asset.EmissionsPerMW);
            Assert.Equal(asset.MaxElectricity < 0, asset.ConsumesElectricity);
            Assert.Equal(asset.MaxElectricity > 0, asset.ProducesElectricity);
        }

        [Fact]
        public void HeatingGrid_ShouldHaveValidData()
        {
            // Assert
            Assert.NotNull(_assetManager.GridInfo);
            Assert.False(string.IsNullOrEmpty(_assetManager.GridInfo.Name));
            Assert.False(string.IsNullOrEmpty(_assetManager.GridInfo.ImageSource));
            Assert.False(string.IsNullOrEmpty(_assetManager.GridInfo.Architecture));
            Assert.False(string.IsNullOrEmpty(_assetManager.GridInfo.Size));
        }
    }
} 