using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.State;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.IntegrationIndexing
{
    /// <summary>
    /// Validates the def allow-list maintenance path of <see cref="MapPolicyManager"/> against the live
    /// ThingFilter index. Verifies that a policy's condition is evaluated once and its allow-list is applied to
    /// every managed storage (including inverted storages) using real <see cref="ThingFilter"/> instances.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class MapPolicyManagerMaintainIntegrationTests : IDisposable
    {
        private const string DefFiltersField = "_defFilters";
        private const string DefCacheField = "_filterToDefCache";
        private const string InvertedDefMapField = "_storageToInvertedDefFilterMap";

        private static readonly ThingDef Def1 = MakeDef("Test_Maintain_Def1");
        private static readonly ThingDef Def2 = MakeDef("Test_Maintain_Def2");
        private static readonly ThingDef Def3 = MakeDef("Test_Maintain_Def3");

        public MapPolicyManagerMaintainIntegrationTests()
        {
            Toolkit.ConfigureServices();

            // Stand up the live ThingFilter index with the persistent storage id and map metadata so the
            // manager can resolve managed storages (mirrors EnableStorageFiltering).
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
        public void ApplyDefPolicyUpdates_WithRealIndexedStoragesSharingPolicy_EvaluatesConditionOncePerDef()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            var defFilter = new StubDefFilter(def => def == Def1 || def == Def3);
            AddToDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var storage1Filter = PushFilter("MaintainStorage1", map);
            var storage2Filter = PushFilter("MaintainStorage2", map);
            AddToDictionary(manager, DefCacheField, storage1Filter, "policyA");
            AddToDictionary(manager, DefCacheField, storage2Filter, "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (storage1Filter, "MaintainStorage1"),
                (storage2Filter, "MaintainStorage2")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, new List<ThingDef> { Def1, Def2, Def3 }, force: false);

            // Assert: the condition is evaluated once per def (not once per storage) and both storages get it.
            Assert.Equal(3, defFilter.FilterCallCount);
            Assert.Contains(Def1, storage1Filter.AllowedThingDefs);
            Assert.Contains(Def3, storage1Filter.AllowedThingDefs);
            Assert.DoesNotContain(Def2, storage1Filter.AllowedThingDefs);
            Assert.Contains(Def1, storage2Filter.AllowedThingDefs);
            Assert.Contains(Def3, storage2Filter.AllowedThingDefs);
            Assert.DoesNotContain(Def2, storage2Filter.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_WithInvertedRealIndexedStorage_FlipsMembership()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            var defFilter = new StubDefFilter(def => def == Def1 || def == Def3);
            AddToDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var storageFilter = PushFilter("MaintainInvertedStorage", map);
            AddToDictionary(manager, DefCacheField, storageFilter, "policyA");
            AddToDictionary(manager, InvertedDefMapField, "MaintainInvertedStorage", "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (storageFilter, "MaintainInvertedStorage")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, new List<ThingDef> { Def1, Def2, Def3 }, force: false);

            // Assert: the inverted storage allows exactly what the policy does not allow.
            Assert.Equal(3, defFilter.FilterCallCount);
            Assert.DoesNotContain(Def1, storageFilter.AllowedThingDefs);
            Assert.Contains(Def2, storageFilter.AllowedThingDefs);
            Assert.DoesNotContain(Def3, storageFilter.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_WithoutVersionChange_DoesNotTouchRealIndexedStorages()
        {
            // Arrange
            var map = CreateMap();
            var manager = new MapPolicyManager(map);

            var defFilter = new StubDefFilter(def => def == Def1, update: () => false);
            AddToDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var storageFilter = PushFilter("MaintainStorageIdle", map);
            AddToDictionary(manager, DefCacheField, storageFilter, "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (storageFilter, "MaintainStorageIdle")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, new List<ThingDef> { Def1, Def2, Def3 }, force: false);

            // Assert
            Assert.Equal(0, defFilter.FilterCallCount);
            Assert.Empty(storageFilter.AllowedThingDefs);
        }

        // ── Helpers ──

        private static Map CreateMap()
            => (Map)FormatterServices.GetUninitializedObject(typeof(Map));

        private static ThingDef MakeDef(string defName)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            // GetUninitializedObject skips field initializers; ThingFilter.SetAllow iterates virtualDefs, so it
            // must be a non-null empty list for the fake def to survive SetAllow.
            typeof(ThingDef).GetField("virtualDefs", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(def, new List<ThingDef>());
            return def;
        }

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
            private readonly Func<ThingDef, bool> _filter;
            private readonly Func<bool> _update;

            public StubDefFilter(Func<ThingDef, bool> filter, Func<bool> update = null)
            {
                _filter = filter;
                _update = update ?? (() => true);
            }

            public int FilterCallCount { get; private set; }

            public Map Scope => null;

            public IDynamicPolicy<Map, ThingDef> Policy => null;

            public bool Filter(ThingDef item)
            {
                FilterCallCount++;
                return _filter(item);
            }

            public bool Update(IStateStore<Map> stateStore) => _update();
        }
    }
}
