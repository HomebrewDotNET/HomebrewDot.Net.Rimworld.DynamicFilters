using System;
using System.Linq;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.IntegrationIndexing
{
    /// <summary>
    /// Reproduces the DynamicFilters storage-filtering indexing flow to verify that the Map
    /// metadata set by the gatherer (persistent: true) actually lands on the indexed ThingFilter.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class ThingFilterMapPersistenceTests : IDisposable
    {
        public ThingFilterMapPersistenceTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
            Toolkit.Indexing.ConfigureSchema -= ConfigureSchema;
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        private static void ConfigureSchema(IDatabaseSchemaBuilder schema)
        {
            schema.WithTable<ThingFilter>(DynamicFiltersToolkit.Indexing.ThingFilter.TableName);
        }

        [Fact]
        public void ThingFilter_PushedWithPersistentMapMetadata_HasMapOnIndexedItem()
        {
            var mapA = (Map)FormatterServices.GetUninitializedObject(typeof(Map));
            var mapB = (Map)FormatterServices.GetUninitializedObject(typeof(Map));
            var filter = new ThingFilter();

            // Mirror EnableStorageFiltering: ensure table, register the Include indexer, then index.
            DynamicFiltersToolkit.Indexing.ThingFilter.EnsureTable();
            Toolkit.Indexing.ConfigureSchema += ConfigureSchema;
            var gatherer = new PushGatherer(filter, mapA, mapB);
            Toolkit.Indexing.ConfigureOrchestrator += b => b.With(gatherer);
            Toolkit.Indexing.Indexers.BuildIndexer<ThingFilter>(ToolkitConstants.Thing.Map.Name, x => x.Include<Map>(ToolkitConstants.Thing.Map, true));
            Toolkit.Indexing.StartIndexing(null, true);

            var table = Toolkit.Indexing.Manager.DatabaseSnapshot?.GetTable<ThingFilter>(DynamicFiltersToolkit.Indexing.ThingFilter.TableName);
            var indexed = table?.Enumerate<IIndexed<ThingFilter>>().FirstOrDefault();

            Assert.NotNull(table);
            Assert.NotNull(indexed);
            Assert.True(indexed.Metadata.ContainsKey(ToolkitConstants.Thing.Map.Name), "Map metadata key should be present on the indexed ThingFilter");
            // The gatherer does NOT mark the metadata persistent: the Include change tracker is what
            // persists it, and it must pick up the latest (second) value pushed for the same filter.
            Assert.Same(mapB, indexed.Metadata[ToolkitConstants.Thing.Map.Name]);
        }

        /// <summary>
        /// Mimics ThingFilterGatherer.Scan: pushes a filter with Map metadata (NOT persistent),
        /// then re-pushes the same filter with a new map to exercise the change tracker.
        /// </summary>
        private class PushGatherer : IDataGatherer
        {
            private readonly ThingFilter _filter;
            private readonly Map _mapA;
            private readonly Map _mapB;

            public PushGatherer(ThingFilter filter, Map mapA, Map mapB)
            {
                _filter = filter;
                _mapA = mapA;
                _mapB = mapB;
            }

            public void GatherData(Game game, ISnapshotManager snapshotManager)
            {
                var metadata = new IndexMetadata();
                metadata.Set(ToolkitConstants.Thing.Map, _mapA);
                snapshotManager.Push(_filter, ref metadata);

                var updated = new IndexMetadata();
                updated.Set(ToolkitConstants.Thing.Map, _mapB);
                snapshotManager.Push(_filter, ref updated);
            }

            public void Initialize(Game game)
            {
            }

            public void Reset()
            {
            }
        }
    }
}
