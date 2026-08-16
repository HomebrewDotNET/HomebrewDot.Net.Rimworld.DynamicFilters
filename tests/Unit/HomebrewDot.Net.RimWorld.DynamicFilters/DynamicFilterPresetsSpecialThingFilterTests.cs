using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Tests for the special thing filter preset helpers on <see cref="DynamicFilterPresets"/>: the condition
    /// factory (<c>Self MatchesThingFilter [SpecialThingFilterDef]</c>), the collection naming, and the duplicate
    /// filter detection that skips filters already covered by a built-in preset.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsSpecialThingFilterTests
    {
        // ═══════════════════════════════════
        // Condition structure
        // ═══════════════════════════════════

        [Fact]
        public void CreateSpecialThingFilterCondition_ComparesSelf()
        {
            var conditions = DynamicFilterPresets.CreateSpecialThingFilterCondition("AllowFresh");

            var condition = Assert.Single(conditions).Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(SelfReferenceType.DefaultTypeName, compare.Type);
        }

        [Fact]
        public void CreateSpecialThingFilterCondition_UsesMatchesThingFilterOperator()
        {
            var conditions = DynamicFilterPresets.CreateSpecialThingFilterCondition("AllowFresh");

            var condition = Assert.Single(conditions).Condition;
            Assert.Equal(MatchesThingFilterOperatorType.DefaultTypeName, condition.With as string);
        }

        [Fact]
        public void CreateSpecialThingFilterCondition_ReferencesSpecialThingFilterDef()
        {
            var conditions = DynamicFilterPresets.CreateSpecialThingFilterCondition("AllowFresh");

            var condition = Assert.Single(conditions).Condition;
            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(DefReferenceType<Verse.SpecialThingFilterDef>.DefaultTypeName, to.Type);
            Assert.Equal("AllowFresh", to.Value);
        }

        [Fact]
        public void CreateSpecialThingFilterCondition_NullDefName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DynamicFilterPresets.CreateSpecialThingFilterCondition(null));
        }

        // ═══════════════════════════════════
        // Duplicate detection
        // ═══════════════════════════════════

        [Theory]
        [InlineData("AllowRotten")]
        [InlineData("AllowCorpsesColonist")]
        [InlineData("AllowCorpsesStranger")]
        [InlineData("AllowCorpsesSlave")]
        [InlineData("AllowCorpsesUnnatural")]
        [InlineData("AllowSmeltable")]
        [InlineData("AllowSmeltableApparel")]
        [InlineData("AllowBiocodedWeapons")]
        [InlineData("AllowBiocodedApparel")]
        public void IsDuplicateSpecialThingFilter_CoveredDefNames_ReturnsTrue(string defName)
        {
            Assert.True(DynamicFilterPresets.IsDuplicateSpecialThingFilter(defName));
        }

        [Theory]
        [InlineData("AllowFresh")]
        [InlineData("AllowNonDeadmansApparel")]
        [InlineData("AllowCorpsesMechFriendly")]
        [InlineData("AllowVegetarian")]
        public void IsDuplicateSpecialThingFilter_UncoveredDefNames_ReturnsFalse(string defName)
        {
            Assert.False(DynamicFilterPresets.IsDuplicateSpecialThingFilter(defName));
        }

        [Fact]
        public void IsDuplicateSpecialThingFilter_Null_ReturnsFalse()
        {
            Assert.False(DynamicFilterPresets.IsDuplicateSpecialThingFilter(null));
        }
    }
}
