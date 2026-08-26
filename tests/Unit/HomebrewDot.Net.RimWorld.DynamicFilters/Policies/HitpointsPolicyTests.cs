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
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    /// <summary>
    /// Tests for the <see cref="HitpointsPolicy"/> template and its settings. Validates the settings that the
    /// hitpoints UI drives: validation of the min/max hitpoint range and the conditions produced by
    /// <see cref="HitpointsPolicy.ConvertOptions"/> from the configured range.
    /// </summary>
    [Trait("Category", "Unit")]
    public class HitpointsPolicyTests
    {
        static HitpointsPolicyTests()
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
            var a = HitpointsPolicy.Instance;
            var b = HitpointsPolicy.Instance;

            // Assert
            Assert.Same(a, b);
        }

        [Fact]
        public void StorageKey_ContainsModId()
        {
            // Act
            var key = HitpointsPolicy.Instance.StorageKey;

            // Assert
            Assert.Contains(DynamicFiltersToolkit.ModId, key);
        }

        [Fact]
        public void ValidateSettings_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => HitpointsPolicy.Instance.ValidateSettings(null).ToList());
        }

        [Fact]
        public void ValidateSettings_WithMinAboveMax_ReturnsError()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MinimumHitpoints = 80, MaximumHitpoints = 20 };

            // Act
            var errors = HitpointsPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.Contains(errors, error => error.Contains("Minimum hitpoints cannot be higher"));
        }

        [Fact]
        public void ValidateSettings_WithNegativeMin_ReturnsError()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MinimumHitpoints = -5 };

            // Act
            var errors = HitpointsPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.Contains(errors, error => error.Contains("cannot be lower than 0"));
        }

        [Fact]
        public void ValidateSettings_WithMaxAbove100_ReturnsError()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MaximumHitpoints = 150 };

            // Act
            var errors = HitpointsPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.Contains(errors, error => error.Contains("cannot be higher than 100"));
        }

        [Fact]
        public void ValidateSettings_WithValidRange_ReturnsNoErrors()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MinimumHitpoints = 30, MaximumHitpoints = 70 };

            // Act
            // Route through the IDynamicPolicyTemplate interface so the converted SimpleFilterPolicy
            // settings (static conditions) are validated, mirroring the in-game UI path.
            HomebrewDot.Net.Rimworld.Configuration.IDynamicPolicyTemplate template = HitpointsPolicy.Instance;
            var errors = template.ValidateSettings(settings).ToList();

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ConvertOptions_WithDefaults_ProducesOnlyApparelWeaponGuard()
        {
            // Arrange
            var settings = new HitpointsPolicySettings();

            // Act
            var converted = HitpointsPolicy.Instance.ConvertOptions(settings);

            // Assert
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(converted);
            Assert.False(typed.ThingDef);
            Assert.False(typed.RequireMapContext);
            Assert.Equal(settings.LazyEvaluation, typed.LazyEvaluation);

            // Full range (0-100) adds no hitpoint conditions, only the apparel-or-weapon guard. When the guard
            // group is the only condition, ConditionBuilder.Build returns the group def directly and its children
            // surface as two top-level conditions carrying the OR chain (behaviorally identical to the group).
            Assert.Equal(2, typed.Conditions.Count);

            var isApparel = typed.Conditions[0].Condition;
            var apparelCompare = Assert.IsAssignableFrom<IReference>(isApparel.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, apparelCompare.Type);
            Assert.Equal($"{nameof(Thing.def)}.{nameof(ThingDef.IsApparel)}", apparelCompare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), isApparel.With as string);
            Assert.True(isApparel.IsOr);

            var isWeapon = typed.Conditions[1].Condition;
            var weaponCompare = Assert.IsAssignableFrom<IReference>(isWeapon.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, weaponCompare.Type);
            Assert.Equal($"{nameof(Thing.def)}.{nameof(ThingDef.IsWeapon)}", weaponCompare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), isWeapon.With as string);
            Assert.False(isWeapon.IsOr);
        }

        [Fact]
        public void ConvertOptions_WithMinAndMax_CreatesHitpointConditions()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MinimumHitpoints = 30, MaximumHitpoints = 70 };

            // Act
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(HitpointsPolicy.Instance.ConvertOptions(settings));

            // Assert
            Assert.Equal(3, typed.Conditions.Count);

            var minCondition = typed.Conditions[1].Condition;
            var minCompare = Assert.IsAssignableFrom<IReference>(minCondition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, minCompare.Type);
            Assert.Equal(ToolkitConstants.Thing.HitPointPercentage.Name, minCompare.Value);
            Assert.Equal(NativeOperatorType.GreaterThanOrEqual.ToOperatorString(), minCondition.With as string);
            Assert.Equal(30f, Assert.IsAssignableFrom<IReference>(minCondition.To).Value);

            var maxCondition = typed.Conditions[2].Condition;
            var maxCompare = Assert.IsAssignableFrom<IReference>(maxCondition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, maxCompare.Type);
            Assert.Equal(ToolkitConstants.Thing.HitPointPercentage.Name, maxCompare.Value);
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), maxCondition.With as string);
            Assert.Equal(70f, Assert.IsAssignableFrom<IReference>(maxCondition.To).Value);
        }

        [Fact]
        public void ConvertOptions_WithOnlyMax_DoesNotCreateMinCondition()
        {
            // Arrange
            var settings = new HitpointsPolicySettings { MaximumHitpoints = 50 };

            // Act
            var typed = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(HitpointsPolicy.Instance.ConvertOptions(settings));

            // Assert
            Assert.Equal(2, typed.Conditions.Count);
            var maxCondition = typed.Conditions[1].Condition;
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), maxCondition.With as string);
        }
    }
}
