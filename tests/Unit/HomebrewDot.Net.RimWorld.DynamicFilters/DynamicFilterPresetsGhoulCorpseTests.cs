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
    /// Tests for the <see cref="DynamicFilterPresets.CreateGhoulCorpseCondition"/> preset conditions.
    /// Verifies both the condition structure (indexed IsGhoulCorpse metadata equals true) and the actual
    /// evaluation behaviour against indexed things carrying the metadata that <c>TrackIsGhoulCorpse</c> sets.
    /// Ghouls are transformed humans, so ghoul corpses share the Human corpse def and the preset is a thing-level
    /// filter that relies on the eager (non-lazy) evaluation path where the indexed items carry that metadata.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsGhoulCorpseTests
    {
        static DynamicFilterPresetsGhoulCorpseTests()
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
        public void CreateGhoulCorpseCondition_ComparesIsGhoulCorpseMetadata()
        {
            var conditions = DynamicFilterPresets.CreateGhoulCorpseCondition();

            var condition = Assert.Single(conditions).Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(ToolkitConstants.Thing.IsGhoulCorpse.Name, compare.Value);
            Assert.False(condition.Inverted);
        }

        [Fact]
        public void CreateGhoulCorpseCondition_UsesEqualsOperator_AgainstTrue()
        {
            var conditions = DynamicFilterPresets.CreateGhoulCorpseCondition();

            var condition = Assert.Single(conditions).Condition;
            Assert.Equal(EqualsOperatorType.DefaultTypeName, condition.With as string);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            Assert.Equal(true, to.Value);
        }

        [Fact]
        public void CreateGhoulCorpseCondition_NotInvertedByDefault()
        {
            Assert.All(DynamicFilterPresets.CreateGhoulCorpseCondition(), c => Assert.False(c.Condition.Inverted));
        }

        // ═══════════════════════════════════
        // Behavioural tests (indexed things with IsGhoulCorpse metadata through the comparator pipeline)
        // ═══════════════════════════════════

        [Fact]
        public void GhoulCorpseCondition_MetadataTrue_Matches()
        {
            var (sut, collection) = BuildEvaluator();
            var indexed = MakeIndexedThing(new Dictionary<string, object>
            {
                [ToolkitConstants.Thing.IsGhoulCorpse.Name] = true
            });

            Assert.True(Matches(sut, collection, indexed));
        }

        [Fact]
        public void GhoulCorpseCondition_MetadataFalse_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var indexed = MakeIndexedThing(new Dictionary<string, object>
            {
                [ToolkitConstants.Thing.IsGhoulCorpse.Name] = false
            });

            Assert.False(Matches(sut, collection, indexed));
        }

        [Fact]
        public void GhoulCorpseCondition_MissingMetadata_DoesNotMatch()
        {
            var (sut, collection) = BuildEvaluator();
            var indexed = MakeIndexedThing(new Dictionary<string, object>());

            Assert.False(Matches(sut, collection, indexed));
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

            var conditions = DynamicFilterPresets.CreateGhoulCorpseCondition();

            // Re-add the conditions exactly like SimpleFilterPolicy.Provider does.
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var condition in conditions)
            {
                _ = cBuilder.CompareFrom(condition.Condition);
            }

            return (new CollectionComparator(conditionComparator), collectionBuilder.Collection);
        }

        private static bool Matches(CollectionComparator sut, CollectionDef collection, Indexed<Thing> item)
        {
            return sut.Matches(collection, item, new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());
        }

        private static Indexed<Thing> MakeIndexedThing(IReadOnlyDictionary<string, object> metadata)
        {
            // The condition only reads the IsGhoulCorpse metadata, so the underlying value just needs to be a
            // non-null Thing. GetUninitializedObject skips the constructor, which avoids Unity stat initialization
            // (e.g. ShaderDatabase) that a real def or thing construction would trigger in a pure .NET runner.
            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            return new Indexed<Thing>(thing, metadata);
        }
    }
}
