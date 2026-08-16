using System;
using System.Collections.Generic;
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
    /// Tests for the <see cref="DynamicFilterPresets.CreateRottingCondition"/> preset conditions.
    /// Verifies both the condition structure (CompRottable stage in Rotting/Dessicated) and the actual
    /// evaluation behaviour against real game objects, including fully decomposed corpses (skeletons).
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsRottingTests
    {
        static DynamicFilterPresetsRottingTests()
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
        public void CreateRottingCondition_ComparesCompRottableStage()
        {
            var conditions = DynamicFilterPresets.CreateRottingCondition();

            var condition = Assert.Single(conditions).Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(CompReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal($"{typeof(CompRottable).FullName}{CompReferenceType.PathSeparator}{nameof(CompRottable.Stage)}", compare.Value);
            Assert.False(condition.Inverted);
        }

        [Fact]
        public void CreateRottingCondition_UsesInOperator_WithRottingAndDessicatedStages()
        {
            var conditions = DynamicFilterPresets.CreateRottingCondition();

            var condition = Assert.Single(conditions).Condition;
            Assert.Equal(InOperatorType.DefaultTypeName, condition.With as string);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            var stages = Assert.IsAssignableFrom<RotStage[]>(to.Value);
            Assert.Contains(RotStage.Rotting, stages);
            // Fully decomposed corpses (skeletons) are RotStage.Dessicated and must be included.
            Assert.Contains(RotStage.Dessicated, stages);
        }

        [Fact]
        public void CreateRottingCondition_NotInvertedByDefault()
        {
            Assert.All(DynamicFilterPresets.CreateRottingCondition(), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (real game objects through the comparator pipeline)
        // ═══════════════════════════════════

        [Fact]
        public void RottingCondition_RottingStage_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var thing = MakeRottableThing(RotStage.Rotting);

            Assert.True(Matches(sut, collection, thing));
        }

        [Fact]
        public void RottingCondition_DessicatedSkeleton_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var thing = MakeRottableThing(RotStage.Dessicated);

            Assert.True(Matches(sut, collection, thing));
        }

        [Fact]
        public void RottingCondition_FreshStage_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var thing = MakeRottableThing(RotStage.Fresh);

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

            var conditions = DynamicFilterPresets.CreateRottingCondition();

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

        private static void SetField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        private static ThingWithComps MakeRottableThing(RotStage stage)
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = "Test_RottableThing";

            var props = new CompProperties_Rottable();
            props.daysToRotStart = 1f;
            props.daysToDessicated = 2f;

            var thing = MakeUninitialized<ThingWithComps>();
            thing.def = def;

            var comp = new CompRottable();
            SetField(typeof(ThingComp), comp, "props", props);
            SetField(typeof(ThingComp), comp, "parent", thing);
            SetField(typeof(ThingWithComps), thing, "comps", new List<ThingComp> { comp });
            SetField(typeof(ThingWithComps), thing, "compsByType", new Dictionary<Type, ThingComp[]>
            {
                [typeof(CompRottable)] = new[] { comp }
            });

            // Rot progress determines the stage: below TicksToRotStart is Fresh, below TicksToDessicated
            // is Rotting, and at or above TicksToDessicated is Dessicated (a fully decomposed skeleton).
            float progress = stage switch
            {
                RotStage.Fresh => props.TicksToRotStart * 0.5f,
                RotStage.Rotting => (props.TicksToRotStart + props.TicksToDessicated) / 2f,
                RotStage.Dessicated => props.TicksToDessicated * 1.5f,
                _ => 0f
            };
            SetField(typeof(CompRottable), comp, "rotProgressInt", progress);

            return thing;
        }
    }
}
