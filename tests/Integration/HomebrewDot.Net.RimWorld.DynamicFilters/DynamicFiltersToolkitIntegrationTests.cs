using System;
using HomebrewDot.Net.Rimworld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Integration tests for the <see cref="DynamicFiltersToolkit"/> entry point that do not require a real
    /// <see cref="DynamicFiltersToolkit"/> instance. <see cref="DynamicFiltersToolkit.ConfigureServices"/> is not
    /// called because it needs the singleton instance (which requires a <see cref="ModContentPack"/>).
    /// </summary>
    [Trait("Category", "Integration")]
    public class DynamicFiltersToolkitIntegrationTests : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void DynamicFiltersToolkit_ModId_IsExpectedPackageId()
        {
            // Act
            var modId = DynamicFiltersToolkit.ModId;

            // Assert
            Assert.Equal("homebrewdot.net.rimworld.dynamicfilters", modId);
        }

        [Fact]
        public void DynamicFiltersToolkit_Instance_WhenNotInitialized_ThrowsArgumentNullException()
        {
            // Act & Assert - the singleton is never set up in tests (needs a ModContentPack)
            Assert.Throws<ArgumentNullException>(() => _ = DynamicFiltersToolkit.Instance);
        }

        [Fact]
        public void DynamicFiltersToolkit_Settings_WhenNotInitialized_ThrowsArgumentNullException()
        {
            // Act & Assert - Settings initializes through the singleton instance
            Assert.Throws<ArgumentNullException>(() => _ = DynamicFiltersToolkit.Settings);
        }

        [Fact]
        public void DynamicFiltersToolkitSettings_Defaults_StorageFilteringOnPresetsOff()
        {
            // Arrange
            var settings = new DynamicFiltersToolkit.DynamicFiltersToolkitSettings();

            // Assert - defaults: storage filtering on, all presets and extras off, no active templates
            Assert.True(settings.EnableStorageFiltering);
            Assert.False(settings.EnablePresets);
            Assert.False(settings.EnableSpecialThingFilterPresets);
            Assert.False(settings.ShowPoliciesButton);
            Assert.Empty(settings.ActiveTemplates);
        }

        [Fact]
        public void DynamicFiltersToolkitSettings_Changed_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DynamicFiltersToolkit.DynamicFiltersToolkitSettings.Changed(null));
        }

        [Fact]
        public void Indexing_ThingFilter_GetCurrentTable_WhenNoDatabase_ReturnsNull()
        {
            // Act - no indexing has been configured, so no live database table exists
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();

            // Assert
            Assert.Null(table);
        }

        [Fact]
        public void Indexing_ThingFilter_GetTable_WhenNoSnapshot_ReturnsNull()
        {
            // Act - no snapshot has been taken, so no snapshot table exists
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();

            // Assert
            Assert.Null(table);
        }
    }
}
