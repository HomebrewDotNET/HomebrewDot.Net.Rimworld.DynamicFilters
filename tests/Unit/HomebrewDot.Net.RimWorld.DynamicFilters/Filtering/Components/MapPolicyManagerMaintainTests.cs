using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.State;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Components
{
    /// <summary>
    /// Tests for the def allow-list maintenance path of <see cref="MapPolicyManager"/> (the "saving policy
    /// takes long time" fix). Verifies the policy condition is evaluated once per def (not once per storage),
    /// inverted storages flip membership, passes without a policy version change are no-ops (no
    /// double-application), and a throwing policy update does not abort the pass.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MapPolicyManagerMaintainTests
    {
        private const string DefFiltersField = "_defFilters";
        private const string DefCacheField = "_filterToDefCache";
        private const string InvertedDefMapField = "_storageToInvertedDefFilterMap";

        private static readonly ThingDef Def1 = MakeDef("Test_Def1");
        private static readonly ThingDef Def2 = MakeDef("Test_Def2");
        private static readonly ThingDef Def3 = MakeDef("Test_Def3");
        private static readonly List<ThingDef> AllDefs = new List<ThingDef> { Def1, Def2, Def3 };

        [Fact]
        public void ApplyDefPolicyUpdates_WithMultipleStoragesSharingPolicy_EvaluatesConditionOncePerDef()
        {
            // Arrange
            var manager = CreateUninitializedManager();
            var defFilter = new CountingDefFilter(def => def == Def1 || def == Def3);
            SetDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var filter1 = new ThingFilter();
            var filter2 = new ThingFilter();
            SetDictionary(manager, DefCacheField, filter1, "policyA");
            SetDictionary(manager, DefCacheField, filter2, "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (filter1, "storage1"),
                (filter2, "storage2")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false);

            // Assert: the condition is evaluated once per def, not once per storage.
            Assert.Equal(3, defFilter.FilterCallCount);
            Assert.Contains(Def1, filter1.AllowedThingDefs);
            Assert.Contains(Def3, filter1.AllowedThingDefs);
            Assert.DoesNotContain(Def2, filter1.AllowedThingDefs);
            Assert.Contains(Def1, filter2.AllowedThingDefs);
            Assert.Contains(Def3, filter2.AllowedThingDefs);
            Assert.DoesNotContain(Def2, filter2.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_WithInvertedStorage_FlipsMembership()
        {
            // Arrange
            var manager = CreateUninitializedManager();
            var defFilter = new CountingDefFilter(def => def == Def1 || def == Def3);
            SetDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var filter = new ThingFilter();
            SetDictionary(manager, DefCacheField, filter, "policyA");
            SetDictionary(manager, InvertedDefMapField, "storage1", "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (filter, "storage1")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false);

            // Assert: the inverted storage allows everything the policy does not allow.
            Assert.DoesNotContain(Def1, filter.AllowedThingDefs);
            Assert.Contains(Def2, filter.AllowedThingDefs);
            Assert.DoesNotContain(Def3, filter.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_WithoutVersionChange_IsNoOp()
        {
            // Arrange
            var manager = CreateUninitializedManager();
            var defFilter = new CountingDefFilter(def => def == Def1, update: () => false);
            SetDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var filter = new ThingFilter();
            SetDictionary(manager, DefCacheField, filter, "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (filter, "storage1")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false);

            // Assert
            Assert.Equal(0, defFilter.FilterCallCount);
            Assert.Empty(filter.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_AfterVersionChange_SecondPassIsNoOp()
        {
            // Arrange: Update reports a version change exactly once (like a collector that was re-populated),
            // so the allow-list must be applied exactly once and never re-evaluated afterwards.
            var manager = CreateUninitializedManager();
            var updateCalls = 0;
            var defFilter = new CountingDefFilter(def => def == Def1, update: () => ++updateCalls == 1);
            SetDictionary(manager, DefFiltersField, "policyA", (IDynamicFilter<Map, ThingDef>)defFilter);

            var filter = new ThingFilter();
            SetDictionary(manager, DefCacheField, filter, "policyA");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (filter, "storage1")
            };

            // Act
            manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false);
            var filterCallsAfterFirstPass = defFilter.FilterCallCount;
            manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false);

            // Assert: evaluated once, applied once, second pass is a no-op.
            Assert.Equal(3, filterCallsAfterFirstPass);
            Assert.Equal(filterCallsAfterFirstPass, defFilter.FilterCallCount);
            Assert.Contains(Def1, filter.AllowedThingDefs);
        }

        [Fact]
        public void ApplyDefPolicyUpdates_WithThrowingUpdate_ContinuesWithOtherBindings()
        {
            // Arrange
            var manager = CreateUninitializedManager();
            var throwingFilter = new CountingDefFilter(def => true, update: () => throw new InvalidOperationException("boom"));
            var healthyFilter = new CountingDefFilter(def => def == Def1);
            SetDictionary(manager, DefFiltersField, "policyThrowing", (IDynamicFilter<Map, ThingDef>)throwingFilter);
            SetDictionary(manager, DefFiltersField, "policyHealthy", (IDynamicFilter<Map, ThingDef>)healthyFilter);

            var throwingStorage = new ThingFilter();
            var healthyStorage = new ThingFilter();
            SetDictionary(manager, DefCacheField, throwingStorage, "policyThrowing");
            SetDictionary(manager, DefCacheField, healthyStorage, "policyHealthy");

            var bindings = new List<(ThingFilter Filter, string StorageId)>
            {
                (throwingStorage, "storageThrowing"),
                (healthyStorage, "storageHealthy")
            };

            // Act
            var ex = Record.Exception(() => manager.ApplyDefPolicyUpdates(bindings, AllDefs, force: false));

            // Assert: the throwing policy is skipped, the healthy one is still applied.
            Assert.Null(ex);
            Assert.Empty(throwingStorage.AllowedThingDefs);
            Assert.Contains(Def1, healthyStorage.AllowedThingDefs);
        }

        // ── Helpers ──

        private static MapPolicyManager CreateUninitializedManager()
        {
            var manager = (MapPolicyManager)FormatterServices.GetUninitializedObject(typeof(MapPolicyManager));
            typeof(MapComponent).GetField("map", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(manager, (Map)FormatterServices.GetUninitializedObject(typeof(Map)));
            SetDictionary<string, IDynamicFilter<Map, ThingDef>>(manager, DefFiltersField, null, null);
            SetDictionary<ThingFilter, string>(manager, DefCacheField, null, null);
            SetDictionary<string, string>(manager, InvertedDefMapField, null, null);
            return manager;
        }

        private static void SetDictionary<TKey, TValue>(MapPolicyManager manager, string fieldName, TKey key, TValue value)
        {
            var field = typeof(MapPolicyManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var dictionary = (Dictionary<TKey, TValue>)(field.GetValue(manager) ?? new Dictionary<TKey, TValue>());
            if (key != null)
            {
                dictionary[key] = value;
            }
            field.SetValue(manager, dictionary);
        }

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

        private sealed class CountingDefFilter : IDynamicFilter<Map, ThingDef>
        {
            private readonly Func<ThingDef, bool> _filter;
            private readonly Func<bool> _update;

            public CountingDefFilter(Func<ThingDef, bool> filter, Func<bool> update = null)
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
