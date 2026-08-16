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
    /// Tests for the <see cref="DynamicFilterPresets.CreateEquipmentCondition"/> preset condition backing the
    /// Equipment preset. Verifies both the condition structure (weapons, primary-slot equipment such as shields,
    /// or apparel) and the actual evaluation behaviour against real game objects, including defs that match on only
    /// one of the three branches and defs that are none of them.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsEquipmentTests
    {
        static DynamicFilterPresetsEquipmentTests()
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
        public void CreateEquipmentCondition_ReturnsThreeConditions()
        {
            var conditions = DynamicFilterPresets.CreateEquipmentCondition();

            Assert.Equal(3, conditions.Length);
        }

        [Fact]
        public void CreateEquipmentCondition_FirstBranchMatchesWeapons()
        {
            var conditions = DynamicFilterPresets.CreateEquipmentCondition();

            var condition = conditions[0].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(nameof(ThingDef.IsWeapon), compare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), condition.With as string);
            Assert.True(condition.IsOr);
        }

        [Fact]
        public void CreateEquipmentCondition_SecondBranchMatchesPrimaryEquipment()
        {
            var conditions = DynamicFilterPresets.CreateEquipmentCondition();

            var condition = conditions[1].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(nameof(ThingDef.equipmentType), compare.Value);
            Assert.Equal(NativeOperatorType.Equal.ToOperatorString(), condition.With as string);
            Assert.True(condition.IsOr);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            Assert.Equal(EquipmentType.Primary, to.Value);
        }

        [Fact]
        public void CreateEquipmentCondition_ThirdBranchMatchesApparel()
        {
            var conditions = DynamicFilterPresets.CreateEquipmentCondition();

            var condition = conditions[2].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(nameof(ThingDef.IsApparel), compare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), condition.With as string);
            Assert.False(condition.IsOr);
        }

        [Fact]
        public void CreateEquipmentCondition_NotInvertedByDefault()
        {
            Assert.All(DynamicFilterPresets.CreateEquipmentCondition(), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (real game objects through the comparator pipeline)
        // ═══════════════════════════════════

        [Fact]
        public void EquipmentCondition_Weapon_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeEquipmentDef(isWeapon: true, isPrimary: false, isApparel: false);

            Assert.True(Matches(sut, collection, def));
        }

        [Fact]
        public void EquipmentCondition_PrimaryEquipmentWithoutVerbsOrTools_Matches()
        {
            // A shield-like def: equippable in the primary slot but with no attack tools or verbs, so
            // IsWeapon is false and only the equipmentType branch matches.
            var (sut, collection) = BuildEvaluator();
            var def = MakeEquipmentDef(isWeapon: false, isPrimary: true, isApparel: false);

            Assert.True(Matches(sut, collection, def));
        }

        [Fact]
        public void EquipmentCondition_Apparel_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeEquipmentDef(isWeapon: false, isPrimary: false, isApparel: true);

            Assert.True(Matches(sut, collection, def));
        }

        [Fact]
        public void EquipmentCondition_MultipleBranches_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeEquipmentDef(isWeapon: true, isPrimary: true, isApparel: true);

            Assert.True(Matches(sut, collection, def));
        }

        [Fact]
        public void EquipmentCondition_NonEquipment_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeEquipmentDef(isWeapon: false, isPrimary: false, isApparel: false);

            Assert.False(Matches(sut, collection, def));
        }

        // ═══════════════════════════════════
        // Helpers
        // ═══════════════════════════════════

        private static (CollectionComparator SUT, CollectionDef Collection) BuildEvaluator()
        {
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var conditions = DynamicFilterPresets.CreateEquipmentCondition();

            // Re-add the conditions exactly like SimpleFilterPolicy.Provider does.
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var condition in conditions)
            {
                _ = cBuilder.CompareFrom(condition.Condition);
            }

            return (new CollectionComparator(conditionComparator), collectionBuilder.Collection);
        }

        private static bool Matches(CollectionComparator sut, CollectionDef collection, ThingDef def)
        {
            // The def-level collection path evaluates IIndexed<T> snapshot entries, exactly like the rows the
            // def gatherer produces for ThingDef-indexed collections.
            var indexed = new Indexed<ThingDef>(def, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            return sut.Matches(collection, indexed, new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());
        }

        private static T MakeUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));

        private static ThingDef MakeEquipmentDef(bool isWeapon, bool isPrimary, bool isApparel)
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = "Test_Equipment";
            def.category = ThingCategory.Item;
            if (isWeapon)
            {
                def.tools = new List<Tool> { new Tool() };
            }
            if (isPrimary)
            {
                def.equipmentType = EquipmentType.Primary;
            }
            if (isApparel)
            {
                def.apparel = new ApparelProperties();
            }
            return def;
        }
    }
}
