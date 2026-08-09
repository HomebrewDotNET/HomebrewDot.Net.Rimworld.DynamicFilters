using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Tests for the <see cref="DynamicFilterPresets.CreateTechLevelCondition"/> preset conditions.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsTechLevelTests
    {
        [Theory]
        [InlineData(GreaterOperatorType.DefaultTypeName)]
        [InlineData(LesserOperatorType.DefaultTypeName)]
        public void CreateTechLevelCondition_ExcludesUndefinedTechLevel(string operatorName)
        {
            var conditions = DynamicFilterPresets.CreateTechLevelCondition(operatorName);

            Assert.Equal(3, conditions.Length);
            var exclusion = conditions[0].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(exclusion.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal("def.techLevel", compare.Value);
            Assert.Equal(NotEqualsOperatorType.Operator.ToOperatorString(), exclusion.With as string);
            var to = Assert.IsAssignableFrom<IReference>(exclusion.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            Assert.Equal(TechLevel.Undefined, to.Value);
        }

        [Theory]
        [InlineData(GreaterOperatorType.DefaultTypeName)]
        [InlineData(LesserOperatorType.DefaultTypeName)]
        public void CreateTechLevelCondition_GuardsAgainstMissingParentFaction(string operatorName)
        {
            var conditions = DynamicFilterPresets.CreateTechLevelCondition(operatorName);

            Assert.Equal(3, conditions.Length);
            var guard = conditions[1].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(guard.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal("Map.ParentFaction", compare.Value);
            Assert.Equal(NotNullOperatorType.DefaultTypeName, guard.With as string);
        }

        [Theory]
        [InlineData(GreaterOperatorType.DefaultTypeName)]
        [InlineData(LesserOperatorType.DefaultTypeName)]
        public void CreateTechLevelCondition_CompareIsDefTechLevelIndexed(string operatorName)
        {
            var conditions = DynamicFilterPresets.CreateTechLevelCondition(operatorName);

            var condition = conditions[2].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal("def.techLevel", compare.Value);
            Assert.Equal(operatorName, condition.With as string);
        }

        [Theory]
        [InlineData(GreaterOperatorType.DefaultTypeName)]
        [InlineData(LesserOperatorType.DefaultTypeName)]
        public void CreateTechLevelCondition_ToIsMapParentFactionTechLevelIndexed(string operatorName)
        {
            var conditions = DynamicFilterPresets.CreateTechLevelCondition(operatorName);

            var condition = conditions[2].Condition;
            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, to.Type);
            Assert.Equal("Map.ParentFaction.def.techLevel", to.Value);
        }

        [Fact]
        public void CreateTechLevelCondition_NotInvertedByDefault()
        {
            var conditions = DynamicFilterPresets.CreateTechLevelCondition(GreaterOperatorType.DefaultTypeName);

            Assert.All(conditions, c => Assert.False(c.Condition.Inverted));
        }
    }
}
