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
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Tests for the <see cref="DynamicFilterPresets.CreateLowQualityCondition"/> preset settings backing the
    /// Low Quality preset. Verifies both the settings (maximum quality below Normal) and the conditions produced
    /// by <see cref="QualityPolicy.ConvertOptions"/> (CompQuality guard plus quality at or below Poor), and the
    /// actual evaluation behaviour against real game objects, including things without a quality comp.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsLowQualityTests
    {
        static DynamicFilterPresetsLowQualityTests()
        {
            // Register the same reference/operator services that Toolkit.ConfigureServices() registers
            // (that method is internal to the Toolkit assembly and exposed to this test assembly via
            // InternalsVisibleTo).
            Toolkit.ConfigureServices();
        }

        // ═══════════════════════════════════
        // Settings tests
        // ═══════════════════════════════════

        [Fact]
        public void CreateLowQualityCondition_ReturnsQualityPolicySettings()
        {
            var settings = DynamicFilterPresets.CreateLowQualityCondition();

            Assert.IsType<QualityPolicySettings>(settings);
            Assert.Equal(QualityCategory.Awful, settings.MinimumQuality);
            Assert.Equal(QualityCategory.Poor, settings.MaximumQuality);
        }

        [Fact]
        public void CreateLowQualityCondition_SetsLazyEvaluation()
        {
            var settings = DynamicFilterPresets.CreateLowQualityCondition();

            Assert.True(settings.LazyEvaluation);
        }

        // ═══════════════════════════════════
        // Structural tests (converted conditions)
        // ═══════════════════════════════════

        [Fact]
        public void ConvertOptions_GuardsAgainstMissingQualityComp()
        {
            var conditions = ConvertedConditions();

            Assert.Equal(2, conditions.Count);
            var guard = conditions[0].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(guard.Compare);
            Assert.Equal(CompReferenceType.DefaultTypeName, compare.Type);
            // A Type input to Comp() is stored on the reference as the Type itself.
            Assert.Equal(typeof(CompQuality), compare.Value);
            Assert.Equal(NotNullOperatorType.DefaultTypeName, guard.With as string);
            Assert.False(guard.Inverted);
        }

        [Fact]
        public void ConvertOptions_ComparesCompQualityQualityWithLessThanOrEqualPoor()
        {
            var conditions = ConvertedConditions();

            var condition = conditions[1].Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(CompReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal($"{typeof(CompQuality).FullName}{CompReferenceType.PathSeparator}{nameof(CompQuality.Quality)}", compare.Value);
            Assert.Equal(NativeOperatorType.LessThanOrEqual.ToOperatorString(), condition.With as string);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(QualityCategory.Poor, to.Value);
        }

        [Fact]
        public void ConvertOptions_NotInvertedByDefault()
        {
            Assert.All(ConvertedConditions(), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (real game objects through the comparator pipeline)
        // ═══════════════════════════════════

        [Theory]
        [InlineData(QualityCategory.Awful, true)]
        [InlineData(QualityCategory.Poor, true)]
        [InlineData(QualityCategory.Normal, false)]
        [InlineData(QualityCategory.Good, false)]
        [InlineData(QualityCategory.Excellent, false)]
        public void LowQualityCondition_QualityCategory_MatchesOnlyBelowNormal(QualityCategory quality, bool expected)
        {
            var (sut, collection) = BuildEvaluator();
            var thing = MakeQualityThing(quality);

            Assert.Equal(expected, Matches(sut, collection, thing));
        }

        [Fact]
        public void LowQualityCondition_NoQualityComp_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var thing = MakeNoQualityThing();

            Assert.False(Matches(sut, collection, thing));
        }

        // ═══════════════════════════════════
        // Helpers
        // ═══════════════════════════════════

        private static List<SimpleFilterPolicyCondition> ConvertedConditions()
        {
            var settings = DynamicFilterPresets.CreateLowQualityCondition();
            return Assert.IsAssignableFrom<SimpleFilterPolicySettings>(QualityPolicy.Instance.ConvertOptions(settings)).Conditions;
        }

        private static (CollectionComparator SUT, CollectionDef Collection) BuildEvaluator()
        {
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var conditions = ConvertedConditions();

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

        private static ThingWithComps MakeQualityThing(QualityCategory quality)
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = "Test_QualityThing";

            var thing = MakeUninitialized<ThingWithComps>();
            thing.def = def;

            var comp = new CompQuality();
            SetField(typeof(ThingComp), comp, "parent", thing);
            SetField(typeof(ThingWithComps), thing, "comps", new List<ThingComp> { comp });
            SetField(typeof(ThingWithComps), thing, "compsByType", new Dictionary<Type, ThingComp[]>
            {
                [typeof(CompQuality)] = new[] { comp }
            });

            // CompQuality.Quality reads the private qualityInt field (a QualityCategory in 1.6).
            SetField(typeof(CompQuality), comp, "qualityInt", quality);

            return thing;
        }

        private static ThingWithComps MakeNoQualityThing()
        {
            var def = MakeUninitialized<ThingDef>();
            def.defName = "Test_NoQualityThing";

            var thing = MakeUninitialized<ThingWithComps>();
            thing.def = def;

            // A real thing always has its comps populated, and Verse.ThingWithComps.GetComp<T> has a fast
            // path that reads comps[0] when comps.Count < 3, so the list must not be empty. Give it a comp
            // that is not CompQuality so GetComp<CompQuality> resolves to null.
            var otherComp = new CompRottable();
            SetField(typeof(ThingComp), otherComp, "parent", thing);
            SetField(typeof(ThingWithComps), thing, "comps", new List<ThingComp> { otherComp });
            SetField(typeof(ThingWithComps), thing, "compsByType", new Dictionary<Type, ThingComp[]>
            {
                [typeof(CompRottable)] = new[] { otherComp }
            });

            return thing;
        }
    }
}
