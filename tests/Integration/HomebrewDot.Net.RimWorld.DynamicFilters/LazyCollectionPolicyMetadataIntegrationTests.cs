using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Policies.Components;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.IntegrationIndexing
{
    /// <summary>
    /// Validates that lazy thing policies resolve metadata-set conditions (corpse-kind presets, etc.) by looking
    /// up the indexed entry in the live database, which is kept current as things spawn. This is what keeps the
    /// corpse presets latency-free: a fresh corpse is filtered immediately instead of waiting for the next
    /// snapshot cycle (Rare tick / Long tick when slow gathering is enabled).
    /// Uses the same setup as the Toolkit devs' integration tests (see
    /// CollectionIntegrationTests.WhenFilterIsCreatedOnDefName_CorrectDefsAreFiltered): the real indexing
    /// pipeline is stood up and uninitialized fixtures are pushed through the real snapshot manager, so the policy
    /// reads the exact live database production uses.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class LazyCollectionPolicyMetadataIntegrationTests : IDisposable
    {
        private static int _thingIdCounter = 1;

        public LazyCollectionPolicyMetadataIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void LazyPolicy_MetadataCondition_ResolvesFromLiveDatabase_WhenMetadataTrue()
        {
            var (policy, thing) = BuildPolicy(metadata: true);

            var filter = ((IDynamicPolicy<Map, Thing>)policy).GetFilter(MakeUninitializedMap());
            Assert.True(((IDynamicFilter<Map, Thing>)filter).Filter(thing));
        }

        [Fact]
        public void LazyPolicy_MetadataCondition_DoesNotMatch_WhenMetadataFalse()
        {
            var (policy, thing) = BuildPolicy(metadata: false);

            var filter = ((IDynamicPolicy<Map, Thing>)policy).GetFilter(MakeUninitializedMap());
            Assert.False(((IDynamicFilter<Map, Thing>)filter).Filter(thing));
        }

        [Fact]
        public void LazyPolicy_MetadataCondition_DoesNotMatch_WhenThingNotIndexed()
        {
            var (policy, _) = BuildPolicy(metadata: true);
            // A thing that was never pushed: Find returns null and the lazy path falls back to the raw thing, where
            // the metadata key does not resolve, so the condition must not match.
            var unindexedThing = MakeUninitializedThing();
            Assert.Null(FindThing(unindexedThing));

            var filter = ((IDynamicPolicy<Map, Thing>)policy).GetFilter(MakeUninitializedMap());
            Assert.False(((IDynamicFilter<Map, Thing>)filter).Filter(unindexedThing));
        }

        private static (LazyCollectionPolicy Policy, Thing Thing) BuildPolicy(bool metadata)
        {
            // Stand up the real indexing pipeline like the devs' integration tests, then push a fixture through the
            // real snapshot manager so the policy reads the exact live database production uses.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var thing = MakeUninitializedThing();
            var indexMetadata = new IndexMetadata();
            // Persistent matches how the real indexers store metadata (TrackedIndexer.Set passes persistent: true),
            // which is what lands on the database row and is resolved by the lazy path.
            indexMetadata.Set(ToolkitConstants.Thing.IsColonistCorpse, metadata, persistent: true);
            Assert.True(Toolkit.Indexing.Manager.Push(thing, ref indexMetadata, allowBuffering: false));

            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var comparer = new CollectionComparator(new Comparator(referenceResolver, operatorTypes));

            var conditions = DynamicFilterPresets.CreateColonistCorpseCondition();
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var condition in conditions)
            {
                _ = cBuilder.CompareFrom(condition.Condition);
            }

            var policy = new LazyCollectionPolicy("TestColonistCorpses", collectionBuilder.Collection, comparer, new Dictionary<string, ICollectionDef>(), LiveThingDatabase());
            return (policy, thing);
        }

        private static IDatabase<Thing> LiveThingDatabase()
            => (Toolkit.Indexing.Manager.Database as IDatabase)?.AsTyped<Thing>();

        private static IIndexed<Thing> FindThing(Thing thing)
            => LiveThingDatabase()?.Find(thing);

        private static Thing MakeUninitializedThing()
        {
            // Each fixture needs a non-null def (Thing.Equals reads def.category) and a unique thingIDNumber
            // (Thing.Equals/GetHashCode are keyed on it and the database table is a dictionary keyed by the thing).
            // GetUninitializedObject skips constructors, which also avoids Unity stat initialization (e.g.
            // ShaderDatabase) in this pure .NET runner.
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            thing.thingIDNumber = _thingIdCounter++;
            return thing;
        }

        private static Map MakeUninitializedMap()
            => (Map)FormatterServices.GetUninitializedObject(typeof(Map));
    }
}
