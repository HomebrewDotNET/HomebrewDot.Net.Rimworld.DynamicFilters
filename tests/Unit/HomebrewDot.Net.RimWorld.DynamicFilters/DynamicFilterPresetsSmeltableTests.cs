using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Tests for the <see cref="DynamicFilterPresets.CreateSmeltableCondition"/> preset conditions.
    /// Verifies both the condition structure (mirroring <see cref="Thing.Smeltable"/>) and the actual
    /// evaluation behaviour against real game objects.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsSmeltableTests
    {
        static DynamicFilterPresetsSmeltableTests()
        {
            // Register the same reference/operator services that Toolkit.ConfigureServices() registers
            // (that method is internal to the Toolkit assembly, so register them here directly).
            Services.Register<IReferenceType>(IndexedReferenceType.Instance, IndexedReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(PropertyReferenceType.Instance, PropertyReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(ValueReferenceType.Instance, ValueReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(StatReferenceType.Instance, StatReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(CompReferenceType.Instance, CompReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(SelfReferenceType.Instance, SelfReferenceType.DefaultTypeName);

            foreach (var alias in EqualsOperatorType.Aliases) Services.Register<IOperatorType>(EqualsOperatorType.Instance, alias);
            foreach (var alias in NotEqualsOperatorType.Aliases) Services.Register<IOperatorType>(NotEqualsOperatorType.Instance, alias);
            foreach (var alias in GreaterOperatorType.Aliases) Services.Register<IOperatorType>(GreaterOperatorType.Instance, alias);
            foreach (var alias in GreaterOrEqualOperatorType.Aliases) Services.Register<IOperatorType>(GreaterOrEqualOperatorType.Instance, alias);
            foreach (var alias in LesserOperatorType.Aliases) Services.Register<IOperatorType>(LesserOperatorType.Instance, alias);
            foreach (var alias in LesserOrEqualOperatorType.Aliases) Services.Register<IOperatorType>(LesserOrEqualOperatorType.Instance, alias);
            foreach (var alias in TrueOperatorType.Aliases) Services.Register<IOperatorType>(TrueOperatorType.Instance, alias);
            foreach (var alias in FalseOperatorType.Aliases) Services.Register<IOperatorType>(FalseOperatorType.Instance, alias);
            foreach (var alias in NullOperatorType.Aliases) Services.Register<IOperatorType>(NullOperatorType.Instance, alias);
            foreach (var alias in NotNullOperatorType.Aliases) Services.Register<IOperatorType>(NotNullOperatorType.Instance, alias);
            foreach (var alias in MatchOperatorType.Aliases) Services.Register<IOperatorType>(MatchOperatorType.Instance, alias);
            Services.Register<IOperatorType>(InOperatorType.Instance, InOperatorType.DefaultTypeName);
            Services.Register<IOperatorType>(ContainsOperatorType.Instance, ContainsOperatorType.DefaultTypeName);
            Services.Register<IOperatorType>(InThingCategoryOperatorType.Instance, InThingCategoryOperatorType.DefaultTypeName);
        }

        // ═══════════════════════════════════
        // Structural tests
        // ═══════════════════════════════════

        [Fact]
        public void CreateSmeltableCondition_True_DefSmeltableMustBeTrue_AndStuffGroup()
        {
            var conditions = DynamicFilterPresets.CreateSmeltableCondition();

            Assert.Equal(2, conditions.Length);

            // def.smeltable == true (AND)
            var defCondition = conditions[0].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(defCondition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal("def.smeltable", compare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), defCondition.With as string);
            Assert.False(defCondition.IsOr);
            Assert.False(defCondition.Inverted);

            // group: !def.MadeFromStuff OR Stuff.smeltable
            var stuffGroup = conditions[1].Condition;
            Assert.NotNull(stuffGroup.Conditions);
            Assert.Equal(2, stuffGroup.Conditions.Length);

            var notMadeFromStuff = stuffGroup.Conditions[0];
            var notMadeFromStuffCompare = Assert.IsAssignableFrom<IReference>(notMadeFromStuff.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, notMadeFromStuffCompare.Type);
            Assert.Equal("def.MadeFromStuff", notMadeFromStuffCompare.Value);
            Assert.Equal(NativeOperatorType.False.ToOperatorString(), notMadeFromStuff.With as string);
            Assert.True(notMadeFromStuff.IsOr);

            var stuffSmeltable = stuffGroup.Conditions[1];
            var stuffSmeltableCompare = Assert.IsAssignableFrom<IReference>(stuffSmeltable.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, stuffSmeltableCompare.Type);
            Assert.Equal("Stuff.smeltable", stuffSmeltableCompare.Value);
            Assert.Equal(NativeOperatorType.True.ToOperatorString(), stuffSmeltable.With as string);
            Assert.False(stuffSmeltable.IsOr);
        }

        [Fact]
        public void CreateSmeltableCondition_NotInvertedByDefault()
        {
            Assert.All(DynamicFilterPresets.CreateSmeltableCondition(), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (real game objects through the comparator pipeline)
        // ═══════════════════════════════════

        [Fact]
        public void SmeltableCondition_NonStuffSmeltableDef_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeDef("Test_SteelChunk", smeltable: true, madeFromStuff: false);
            var thing = MakeThing(def, stuff: null);

            Assert.True(Matches(sut, collection, thing));
        }

        [Fact]
        public void SmeltableCondition_StuffMadeWithSmeltableStuff_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var stuff = MakeDef("Test_SteelStuff", smeltable: true, madeFromStuff: false);
            var def = MakeDef("Test_SteelClub", smeltable: true, madeFromStuff: true);
            var thing = MakeThing(def, stuff);

            Assert.True(Matches(sut, collection, thing));
        }

        [Fact]
        public void SmeltableCondition_StuffMadeWithNonSmeltableStuff_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var stuff = MakeDef("Test_WoodStuff", smeltable: false, madeFromStuff: false);
            var def = MakeDef("Test_WoodenClub", smeltable: true, madeFromStuff: true);
            var thing = MakeThing(def, stuff);

            Assert.False(Matches(sut, collection, thing));
        }

        [Fact]
        public void SmeltableCondition_NonSmeltableDef_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var def = MakeDef("Test_NonSmeltableItem", smeltable: false, madeFromStuff: false);
            var thing = MakeThing(def, stuff: null);

            Assert.False(Matches(sut, collection, thing));
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

            var conditions = DynamicFilterPresets.CreateSmeltableCondition();

            // Re-add the conditions exactly like SimpleFilterPolicy.Provider does.
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var condition in conditions)
            {
                _ = cBuilder.CompareFrom(condition.Condition);
            }

            return (new CollectionComparator(conditionComparator), collectionBuilder.Collection);
        }

        private static bool Matches(CollectionComparator sut, CollectionDef collection, Thing thing)
        {
            return sut.Matches(collection, thing, new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());
        }

        private static T MakeUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));

        private static ThingDef MakeDef(string defName, bool smeltable, bool madeFromStuff)
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = defName;
            def.smeltable = smeltable;
            if (madeFromStuff)
            {
                var stuffCategoriesField = typeof(ThingDef).GetField("stuffCategories", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                stuffCategoriesField.SetValue(def, new List<StuffCategoryDef> { MakeUninitialized<StuffCategoryDef>() });
            }
            return def;
        }

        private static Thing MakeThing(ThingDef def, ThingDef stuff)
        {
            var thing = MakeUninitialized<Thing>();
            thing.def = def;
            if (stuff != null)
            {
                var stuffIntField = typeof(Thing).GetField("stuffInt", BindingFlags.Instance | BindingFlags.NonPublic);
                stuffIntField.SetValue(thing, stuff);
            }
            return thing;
        }
    }
}
