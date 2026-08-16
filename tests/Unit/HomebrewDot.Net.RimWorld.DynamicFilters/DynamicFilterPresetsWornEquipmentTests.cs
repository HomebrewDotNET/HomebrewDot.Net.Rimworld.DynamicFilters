using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Tests for the <see cref="DynamicFilterPresets.CreateWornEquipmentCondition"/> preset conditions backing the
    /// Tattered and Worn Out presets. Verifies both the condition structure (apparel or weapon, hit points at or
    /// below a threshold via the indexed <see cref="ToolkitConstants.Thing.HitPointPercentage"/> metadata) and the
    /// actual evaluation behaviour against real game objects, including things without the metadata and things that
    /// are neither apparel nor weapons.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsWornEquipmentTests
    {
        static DynamicFilterPresetsWornEquipmentTests()
        {
            // Register the same reference/operator services that Toolkit.ConfigureServices() registers
            // (that method is internal to the Toolkit assembly and exposed to this test assembly via
            // InternalsVisibleTo).
            Toolkit.ConfigureServices();
        }

        // ═══════════════════════════════════
        // Structural tests
        // ═══════════════════════════════════

        [Fact]
        public void CreateWornEquipmentCondition_ReturnsTwoConditions()
        {
            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(25f);

            Assert.Equal(2, conditions.Length);
        }

        [Fact]
        public void CreateWornEquipmentCondition_GroupsApparelOrWeapon()
        {
            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(25f);

            var group = conditions[0].Condition;
            Assert.NotNull(group.Conditions);
            Assert.Equal(2, group.Conditions.Length);
            // The group is a pure container: the OR between apparel and weapon is carried by the first
            // child's IsOr flag (this is what the compiled comparison path consumes).

            var isApparel = group.Conditions[0];
            var compareApparel = Assert.IsAssignableFrom<IReference>(isApparel.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compareApparel.Type);
            Assert.Equal($"{nameof(Thing.def)}.{nameof(ThingDef.IsApparel)}", compareApparel.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), isApparel.With as string);
            Assert.True(isApparel.IsOr);

            var isWeapon = group.Conditions[1];
            var compareWeapon = Assert.IsAssignableFrom<IReference>(isWeapon.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compareWeapon.Type);
            Assert.Equal($"{nameof(Thing.def)}.{nameof(ThingDef.IsWeapon)}", compareWeapon.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), isWeapon.With as string);
            Assert.False(isWeapon.IsOr);
        }

        [Fact]
        public void CreateWornEquipmentCondition_ComparesHitPointPercentageWithLessThanOrEqual()
        {
            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(25f);

            var condition = conditions[1].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(ToolkitConstants.Thing.HitPointPercentage.Name, compare.Value);
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), condition.With as string);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            Assert.Equal(25f, to.Value);
        }

        [Fact]
        public void CreateWornEquipmentCondition_UsesProvidedThreshold()
        {
            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(50f);

            var to = Assert.IsAssignableFrom<IReference>(conditions[1].Condition.To);
            Assert.Equal(50f, to.Value);
        }

        [Fact]
        public void CreateWornEquipmentCondition_NotInvertedByDefault()
        {
            Assert.All(DynamicFilterPresets.CreateWornEquipmentCondition(25f), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (real game objects through the comparator pipeline)
        // ═══════════════════════════════════

        [Theory]
        [InlineData(10f, true)]
        [InlineData(25f, true)]
        [InlineData(26f, false)]
        [InlineData(90f, false)]
        public void WornEquipmentCondition_Apparel_MatchingOnlyAtOrBelowThreshold(float hitPointPercentage, bool expected)
        {
            var (sut, collection) = BuildEvaluator(25f);
            var thing = MakeEquipmentThing(isApparel: true, isWeapon: false);

            Assert.Equal(expected, Matches(sut, collection, thing, hitPointPercentage));
        }

        [Theory]
        [InlineData(10f, true)]
        [InlineData(25f, true)]
        [InlineData(50f, false)]
        [InlineData(80f, false)]
        public void WornEquipmentCondition_Weapon_MatchingOnlyAtOrBelowThreshold(float hitPointPercentage, bool expected)
        {
            var (sut, collection) = BuildEvaluator(25f);
            var thing = MakeEquipmentThing(isApparel: false, isWeapon: true);

            Assert.Equal(expected, Matches(sut, collection, thing, hitPointPercentage));
        }

        [Fact]
        public void WornEquipmentCondition_NonEquipmentWithLowHitPoints_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator(25f);
            var thing = MakeEquipmentThing(isApparel: false, isWeapon: false);

            Assert.False(Matches(sut, collection, thing, 10f));
        }

        [Fact]
        public void WornEquipmentCondition_WithoutHitPointPercentageMetadata_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator(25f);
            var thing = MakeEquipmentThing(isApparel: true, isWeapon: false);

            Assert.False(Matches(sut, collection, thing, null));
        }

        [Fact]
        public void WornEquipmentCondition_ThresholdSeparatesWornOutFromTattered()
        {
            var (tatteredSut, tatteredCollection) = BuildEvaluator(25f);
            var (wornOutSut, wornOutCollection) = BuildEvaluator(50f);
            var thing = MakeEquipmentThing(isApparel: true, isWeapon: false);

            // 30% is above the tattered threshold but at or below the worn-out threshold.
            Assert.False(Matches(tatteredSut, tatteredCollection, thing, 30f));
            Assert.True(Matches(wornOutSut, wornOutCollection, thing, 30f));
        }

        // ═══════════════════════════════════
        // Helpers
        // ═══════════════════════════════════

        private static (CollectionComparator SUT, CollectionDef Collection) BuildEvaluator(float maxHitPointPercentage)
        {
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(maxHitPointPercentage);

            // Re-add the conditions exactly like SimpleFilterPolicy.Provider does.
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var condition in conditions)
            {
                _ = cBuilder.CompareFrom(condition.Condition);
            }

            return (new CollectionComparator(conditionComparator), collectionBuilder.Collection);
        }

        private static bool Matches(CollectionComparator sut, CollectionDef collection, Thing thing, float? hitPointPercentage)
        {
            // The eager collection path evaluates IIndexed<T> snapshot entries, so the metadata is carried on the
            // indexed wrapper, exactly like the rows produced by TrackHitPointPercentage in production.
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (hitPointPercentage.HasValue)
            {
                metadata[ToolkitConstants.Thing.HitPointPercentage.Name] = hitPointPercentage.Value;
            }
            var indexed = new Indexed<Thing>(thing, metadata);
            return sut.Matches(collection, indexed, new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());
        }

        private static T MakeUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));

        private static ThingWithComps MakeEquipmentThing(bool isApparel, bool isWeapon)
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = "Test_WornEquipment";
            def.category = ThingCategory.Item;
            def.useHitPoints = true;
            if (isApparel)
            {
                def.apparel = new ApparelProperties();
            }
            if (isWeapon)
            {
                def.tools = new List<Tool> { new Tool() };
            }

            var thing = MakeUninitialized<ThingWithComps>();
            thing.def = def;
            return thing;
        }
    }
}
