using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Patches;
using HomebrewDot.Net.Rimworld.State;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.IntegrationIndexing
{
    /// <summary>
    /// Validates that the vanilla storage settings copy/paste gizmos
    /// (<see cref="RimWorld.StorageSettingsClipboard"/>) transfer dynamic policies along with the filter
    /// allowances. The source's policies are captured by <c>Postfix_Copy</c> and re-applied by
    /// <c>Postfix_PasteInto</c>, using the real ThingFilter index for storage id and map resolution.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class StorageSettingsClipboardPatcherIntegrationTests : IDisposable
    {
        private const string DefMapField = "_storageToDefFilterMap";
        private const string InvertedDefMapField = "_storageToInvertedDefFilterMap";
        private const string ThingMapField = "_storageToThingFilterMap";
        private const string InvertedThingMapField = "_storageToInvertedThingFilterMap";
        private const string ThingFiltersField = "_thingFilters";
        private const string DefFiltersField = "_defFilters";

        public StorageSettingsClipboardPatcherIntegrationTests()
        {
            Toolkit.ConfigureServices();

            // Mirror EnableStorageFiltering: stand up the live ThingFilter index with the persistent
            // storage id and map metadata so the patcher can resolve them at copy/paste time.
            DynamicFiltersToolkit.Indexing.ThingFilter.EnsureTable();
            Toolkit.Indexing.Indexers.BuildIndexer<ThingFilter>(ToolkitConstants.Thing.Map.Name, x => x.Include<Map>(ToolkitConstants.Thing.Map, true));
            Toolkit.Indexing.Indexers.BuildIndexer<ThingFilter>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name, x => x.Include<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, true));
            Toolkit.Indexing.StartIndexing(null, false);
        }

        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void CopyThenPaste_TransfersDefAndThingPoliciesToDestination()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            // The policies are "active" on this map so ManageWith accepts them.
            AddToDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)new StubDefFilter());
            AddToDictionary(manager, ThingFiltersField, "policyB", (IDynamicFilter<Map, Thing>)new StubThingFilter());

            // The source storage is bound to policyA (def, inverted) and policyB (thing, inverted).
            AddToDictionary(manager, DefMapField, "SourceStorage", "policyA");
            AddToDictionary(manager, InvertedDefMapField, "SourceStorage", "policyA");
            AddToDictionary(manager, ThingMapField, "SourceStorage", "policyB");
            AddToDictionary(manager, InvertedThingMapField, "SourceStorage", "policyB");

            var sourceSettings = new StorageSettings { filter = PushFilter("SourceStorage", map) };
            var destinationSettings = new StorageSettings { filter = PushFilter("DestinationStorage", map) };

            // Act
            StorageSettingsClipboardPatcher.Postfix_Copy(sourceSettings);
            StorageSettingsClipboardPatcher.Postfix_PasteInto(destinationSettings);

            // Assert
            Assert.True(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: false, out var defPolicy, out var defInverted));
            Assert.Equal("policyA", defPolicy);
            Assert.True(defInverted);

            Assert.True(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: true, out var thingPolicy, out var thingInverted));
            Assert.Equal("policyB", thingPolicy);
            Assert.True(thingInverted);

            // The source binding is left untouched.
            Assert.True(manager.TryGetPolicyForStorage("SourceStorage", isForThing: false, out var sourceDefPolicy, out _));
            Assert.Equal("policyA", sourceDefPolicy);
        }

        [Fact]
        public void Paste_WhenSourceHasNoPolicies_RemovesDestinationPolicies()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            var sourceSettings = new StorageSettings { filter = PushFilter("SourceStorage", map) };

            // The destination is currently bound to a def policy that the paste must clear.
            AddToDictionary(manager, DefMapField, "DestinationStorage", "policyX");
            var destinationSettings = new StorageSettings { filter = PushFilter("DestinationStorage", map) };

            // Act
            StorageSettingsClipboardPatcher.Postfix_Copy(sourceSettings);
            StorageSettingsClipboardPatcher.Postfix_PasteInto(destinationSettings);

            // Assert
            Assert.False(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: false, out _, out _));
            Assert.False(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: true, out _, out _));
        }

        [Fact]
        public void CopyOfUnindexedFilter_DoesNotCapture_PasteLeavesDestinationUntouched()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            // A filter that is not in the live index (e.g. a non-storage owner) has no storage id.
            var sourceSettings = new StorageSettings { filter = new ThingFilter() };
            StorageSettingsClipboardPatcher.Postfix_Copy(sourceSettings);

            AddToDictionary(manager, DefMapField, "DestinationStorage", "policyX");
            var destinationSettings = new StorageSettings { filter = PushFilter("DestinationStorage", map) };

            // Act
            StorageSettingsClipboardPatcher.Postfix_PasteInto(destinationSettings);

            // Assert
            Assert.True(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: false, out var policy, out _));
            Assert.Equal("policyX", policy);
        }

        [Fact]
        public void CopyWithoutMapMetadata_DoesNotCapture_PasteLeavesDestinationUntouched()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            // Indexed with a storage id but no map: the source map cannot be resolved, so nothing is captured.
            var filter = new ThingFilter();
            var metadata = new IndexMetadata();
            metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, "SourceStorage", persistent: true);
            Assert.True(Toolkit.Indexing.Manager.Push(filter, ref metadata, allowBuffering: false));
            var sourceSettings = new StorageSettings { filter = filter };
            StorageSettingsClipboardPatcher.Postfix_Copy(sourceSettings);

            AddToDictionary(manager, DefMapField, "DestinationStorage", "policyX");
            var destinationSettings = new StorageSettings { filter = PushFilter("DestinationStorage", map) };

            // Act
            StorageSettingsClipboardPatcher.Postfix_PasteInto(destinationSettings);

            // Assert
            Assert.True(manager.TryGetPolicyForStorage("DestinationStorage", isForThing: false, out var policy, out _));
            Assert.Equal("policyX", policy);
        }

        // ── Helpers ──

        private static Map CreateMap()
            => (Map)FormatterServices.GetUninitializedObject(typeof(Map));

        private static ThingFilter PushFilter(string storageId, Map map)
        {
            var filter = new ThingFilter();
            var metadata = new IndexMetadata();
            metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId, persistent: true);
            metadata.Set(ToolkitConstants.Thing.Map, map, persistent: true);
            Assert.True(Toolkit.Indexing.Manager.Push(filter, ref metadata, allowBuffering: false));
            return filter;
        }

        private static void AddToDictionary<TKey, TValue>(MapPolicyManager manager, string fieldName, TKey key, TValue value)
        {
            var field = typeof(MapPolicyManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var dictionary = (Dictionary<TKey, TValue>)field.GetValue(manager);
            Assert.NotNull(dictionary);
            dictionary[key] = value;
        }

        private sealed class StubDefFilter : IDynamicFilter<Map, ThingDef>
        {
            public Map Scope => null;
            public IDynamicPolicy<Map, ThingDef> Policy => null;
            public bool Filter(ThingDef item) => true;
            public bool Update(IStateStore<Map> stateStore) => false;
        }

        private sealed class StubThingFilter : IDynamicFilter<Map, Thing>
        {
            public Map Scope => null;
            public IDynamicPolicy<Map, Thing> Policy => null;
            public bool Filter(Thing item) => true;
            public bool Update(IStateStore<Map> stateStore) => false;
        }
    }
}
