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
    /// Tests for the colonist/stranger/slave/unnatural corpse preset conditions. Verifies both the condition
    /// structure (indexed Is*Corpse metadata equals true) and the actual evaluation behaviour against indexed things
    /// carrying the metadata that <c>TrackCorpseKind</c> sets. These corpse categories can only be determined
    /// per-instance, so the presets are thing-level filters that rely on the eager (non-lazy) evaluation path where
    /// the indexed items carry that metadata.
    /// </summary>
    [Trait("Category", "Unit")]
    public class DynamicFilterPresetsCorpseKindTests
    {
        static DynamicFilterPresetsCorpseKindTests()
        {
            // Register the same reference/operator services that Toolkit.ConfigureServices() registers
            // (that method is internal to the Toolkit assembly and exposed to this test assembly via
            // InternalsVisibleTo).
            Toolkit.ConfigureServices();
        }

        public static IEnumerable<object[]> CorpseKindKeys()
        {
            yield return new object[] { nameof(ToolkitConstants.Thing.IsColonistCorpse), DynamicFilterPresets.CreateColonistCorpseCondition };
            yield return new object[] { nameof(ToolkitConstants.Thing.IsStrangerCorpse), DynamicFilterPresets.CreateStrangerCorpseCondition };
            yield return new object[] { nameof(ToolkitConstants.Thing.IsSlaveCorpse), DynamicFilterPresets.CreateSlaveCorpseCondition };
            yield return new object[] { nameof(ToolkitConstants.Thing.IsUnnaturalCorpse), DynamicFilterPresets.CreateUnnaturalCorpseCondition };
            yield return new object[] { nameof(ToolkitConstants.Thing.IsPetCorpse), DynamicFilterPresets.CreatePetCorpseCondition };
        }

        // ═══════════════════════════════════
        // Structural tests
        // ═══════════════════════════════════

        [Theory]
        [MemberData(nameof(CorpseKindKeys))]
        public void CreateCorpseKindCondition_ComparesIsCorpseKindMetadata(string keyName, Func<SimpleFilterPolicyCondition[]> factory)
        {
            var condition = Assert.Single(factory()).Condition;
            var compare = Assert.IsAssignableFrom<IReference>(condition.Compare);
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compare.Type);
            Assert.Equal(keyName, compare.Value);
            Assert.False(condition.Inverted);
        }

        [Theory]
        [MemberData(nameof(CorpseKindKeys))]
        public void CreateCorpseKindCondition_UsesEqualsOperator_AgainstTrue(string keyName, Func<SimpleFilterPolicyCondition[]> factory)
        {
            var condition = Assert.Single(factory()).Condition;
            Assert.Equal(EqualsOperatorType.DefaultTypeName, condition.With as string);

            var to = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, to.Type);
            Assert.Equal(true, to.Value);
        }

        // ═══════════════════════════════════
        // Behavioural tests (indexed things with corpse-kind metadata through the comparator pipeline)
        // ═══════════════════════════════════

        [Theory]
        [MemberData(nameof(CorpseKindKeys))]
        public void CorpseKindCondition_MetadataTrue_Matches(string keyName, Func<SimpleFilterPolicyCondition[]> factory)
        {
            var (sut, collection) = BuildEvaluator(factory());
            var indexed = MakeIndexedThing(new Dictionary<string, object>
            {
                [keyName] = true
            });

            Assert.True(Matches(sut, collection, indexed));
        }

        [Theory]
        [MemberData(nameof(CorpseKindKeys))]
        public void CorpseKindCondition_MetadataFalse_DoesNotMatch(string keyName, Func<SimpleFilterPolicyCondition[]> factory)
        {
            var (sut, collection) = BuildEvaluator(factory());
            var indexed = MakeIndexedThing(new Dictionary<string, object>
            {
                [keyName] = false
            });

            Assert.False(Matches(sut, collection, indexed));
        }

        [Theory]
        [MemberData(nameof(CorpseKindKeys))]
        public void CorpseKindCondition_MissingMetadata_DoesNotMatch(string keyName, Func<SimpleFilterPolicyCondition[]> factory)
        {
            var (sut, collection) = BuildEvaluator(factory());
            var indexed = MakeIndexedThing(new Dictionary<string, object>());

            Assert.False(Matches(sut, collection, indexed));
        }

        // ═══════════════════════════════════
        // Helpers
        // ═══════════════════════════════════

        private static (CollectionComparator SUT, CollectionDef Collection) BuildEvaluator(SimpleFilterPolicyCondition[] conditions)
        {
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

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
            // The conditions only read the corpse-kind metadata, so the underlying value just needs to be a
            // non-null Thing. GetUninitializedObject skips the constructor, which avoids Unity stat initialization
            // (e.g. ShaderDatabase) that a real def or thing construction would trigger in a pure .NET runner.
            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            return new Indexed<Thing>(thing, metadata);
        }
    }
}
