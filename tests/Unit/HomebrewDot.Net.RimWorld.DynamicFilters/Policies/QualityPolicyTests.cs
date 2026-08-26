using System;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    /// <summary>
    /// Tests for the <see cref="QualityPolicy"/> template and its settings. Validates the settings that the
    /// quality UI drives: validation of the min/max quality range and the conditions produced by
    /// <see cref="QualityPolicy.ConvertOptions"/> from the configured range.
    /// </summary>
    [Trait("Category", "Unit")]
    public class QualityPolicyTests
    {
        static QualityPolicyTests()
        {
            // Register the same reference/operator services that Toolkit.ConfigureServices() registers
            // (that method is internal to the Toolkit assembly and exposed to this test assembly via
            // InternalsVisibleTo).
            Toolkit.ConfigureServices();
        }

        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            // Act
            var a = QualityPolicy.Instance;
            var b = QualityPolicy.Instance;

            // Assert
            Assert.Same(a, b);
        }

        [Fact]
        public void StorageKey_ContainsModId()
        {
            // Act
            var key = QualityPolicy.Instance.StorageKey;

            // Assert
            Assert.Contains(DynamicFiltersToolkit.ModId, key);
        }

        [Fact]
        public void ValidateSettings_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => QualityPolicy.Instance.ValidateSettings(null).ToList());
        }

        [Fact]
        public void ValidateSettings_WithMinAboveMax_ReturnsError()
        {
            // Arrange
            var settings = new QualityPolicySettings { MinimumQuality = QualityCategory.Good, MaximumQuality = QualityCategory.Poor };

            // Act
            var errors = QualityPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.Contains(errors, error => error.Contains("Minimum quality cannot be higher"));
        }

        [Fact]
        public void ValidateSettings_WithValidRange_ReturnsNoErrors()
        {
            // Arrange
            var settings = new QualityPolicySettings { MinimumQuality = QualityCategory.Poor, MaximumQuality = QualityCategory.Good };

            // Act
            // Route through the IDynamicPolicyTemplate interface so the converted SimpleFilterPolicy
            // settings (static conditions) are validated, mirroring the in-game UI path.
            HomebrewDot.Net.Rimworld.Configuration.IDynamicPolicyTemplate template = QualityPolicy.Instance;
            var errors = template.ValidateSettings(settings).ToList();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ConvertOptions_WithDefaults_ProducesOnlyQualityCompGuard()
        {
            // Arrange
            var settings = new QualityPolicySettings();

            // Act
            var converted = QualityPolicy.Instance.ConvertOptions(settings);

            // Assert
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(converted);
            Assert.False(typed.ThingDef);
            Assert.False(typed.RequireMapContext);
            Assert.Equal(settings.LazyEvaluation, typed.LazyEvaluation);

            // Full range (Awful-Legendary) adds no min/max quality conditions, only the quality comp guard.
            var guard = Assert.Single(typed.Conditions).Condition;
            var compare = Assert.IsAssignableFrom<IReference>(guard.Compare);
            Assert.Equal(CompReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(typeof(CompQuality), compare.Value);
            Assert.Equal(NotNullOperatorType.DefaultTypeName, guard.With as string);
        }

        [Fact]
        public void ConvertOptions_WithMinAndMax_CreatesQualityConditions()
        {
            // Arrange
            var settings = new QualityPolicySettings { MinimumQuality = QualityCategory.Poor, MaximumQuality = QualityCategory.Good };

            // Act
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(QualityPolicy.Instance.ConvertOptions(settings));

            // Assert
            Assert.Equal(3, typed.Conditions.Count);

            var minCondition = typed.Conditions[1].Condition;
            var minCompare = Assert.IsAssignableFrom<IReference>(minCondition.Compare);
            Assert.Equal(CompReferenceType.DefaultTypeName, minCompare.Type);
            Assert.Equal($"{typeof(CompQuality).FullName}{CompReferenceType.PathSeparator}{nameof(CompQuality.Quality)}", minCompare.Value);
            Assert.Equal(NativeOperatorType.GreaterThanOrEqual.ToOperatorString(), minCondition.With as string);
            Assert.Equal(QualityCategory.Poor, Assert.IsAssignableFrom<IReference>(minCondition.To).Value);

            var maxCondition = typed.Conditions[2].Condition;
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), maxCondition.With as string);
            Assert.Equal(QualityCategory.Good, Assert.IsAssignableFrom<IReference>(maxCondition.To).Value);
        }

        [Fact]
        public void ConvertOptions_WithOnlyMax_DoesNotCreateMinCondition()
        {
            // Arrange
            var settings = new QualityPolicySettings { MaximumQuality = QualityCategory.Normal };

            // Act
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(QualityPolicy.Instance.ConvertOptions(settings));

            // Assert
            Assert.Equal(2, typed.Conditions.Count);
            var maxCondition = typed.Conditions[1].Condition;
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), maxCondition.With as string);
            Assert.Equal(QualityCategory.Normal, Assert.IsAssignableFrom<IReference>(maxCondition.To).Value);
        }
    }
}
