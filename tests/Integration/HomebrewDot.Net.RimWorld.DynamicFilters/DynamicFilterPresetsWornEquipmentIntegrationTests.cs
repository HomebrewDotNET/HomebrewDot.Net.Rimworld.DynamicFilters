using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Filtering;
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
    /// Validates that eager (non-lazy) worn-equipment presets collect things from the indexed snapshot using the
    /// <see cref="ToolkitConstants.Thing.HitPointPercentage"/> metadata, i.e. the exact path SimpleFilterPolicy takes
    /// when LazyEvaluation is disabled. Snapshot rows carry the metadata that <c>TrackHitPointPercentage</c> writes
    /// in production; here it is set directly on push because the indexer itself depends on the Unity stat system
    /// (<see cref="Thing.MaxHitPoints"/>), which is unavailable in the pure .NET runner.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class DynamicFilterPresetsWornEquipmentIntegrationTests : IDisposable
    {
        private static int _thingIdCounter = 1;
        private readonly List<string> _collectors = new List<string>();

        public DynamicFilterPresetsWornEquipmentIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            foreach (var name in _collectors)
            {
                InvokeSafe(() => Toolkit.Collecting.Remove(name));
            }
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void WornEquipmentPreset_EagerCollection_CollectsOnlyDamagedEquipmentFromSnapshot()
        {
            // Arrange: stand up the real indexing pipeline and push fixtures that carry HitPointPercentage metadata,
            // exactly like the rows TrackHitPointPercentage produces in production.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var tatteredApparel = PushWornThing(isApparel: true, isWeapon: false, hitPointPercentage: 20f);
            var wornApparel = PushWornThing(isApparel: true, isWeapon: false, hitPointPercentage: 50f);
            var healthyApparel = PushWornThing(isApparel: true, isWeapon: false, hitPointPercentage: 90f);
            var tatteredWeapon = PushWornThing(isApparel: false, isWeapon: true, hitPointPercentage: 10f);
            var healthyWeapon = PushWornThing(isApparel: false, isWeapon: true, hitPointPercentage: 80f);
            var genericItem = PushWornThing(isApparel: false, isWeapon: false, hitPointPercentage: 15f);

            // Act: build the eager collection exactly like SimpleFilterPolicy.Provider does for non-lazy presets.
            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(25f);
            var collectionName = $"TestTattered_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(collectionName, x =>
            {
                foreach (var condition in conditions)
                {
                    _ = x.CompareFrom(condition.Condition);
                }
                return x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).GetSnapshot(), false);
            });
            _collectors.Add(collectionName);

            // Assert: only apparel/weapons at or below 25% hit points are collected.
            var collector = (ICollector<Thing>)Toolkit.Collecting.GetAllCollectors()[collectionName];
            var collected = collector.GetAll().ToArray();
            Assert.Contains(tatteredApparel, collected);
            Assert.Contains(tatteredWeapon, collected);
            Assert.DoesNotContain(wornApparel, collected);
            Assert.DoesNotContain(healthyApparel, collected);
            Assert.DoesNotContain(healthyWeapon, collected);
            Assert.DoesNotContain(genericItem, collected);
        }

        [Fact]
        public void WornEquipmentPreset_EagerPolicyFilter_FiltersPerThingFromCollectedSnapshot()
        {
            // Arrange: same pipeline, with a 50% (worn-out) threshold so a moderately damaged item matches.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var wornApparel = PushWornThing(isApparel: true, isWeapon: false, hitPointPercentage: 50f);
            var healthyApparel = PushWornThing(isApparel: true, isWeapon: false, hitPointPercentage: 90f);
            var genericItem = PushWornThing(isApparel: false, isWeapon: false, hitPointPercentage: 10f);

            var conditions = DynamicFilterPresets.CreateWornEquipmentCondition(50f);
            var collectionName = $"TestWornOut_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(collectionName, x =>
            {
                foreach (var condition in conditions)
                {
                    _ = x.CompareFrom(condition.Condition);
                }
                return x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).GetSnapshot(), false);
            });
            _collectors.Add(collectionName);

            // Act: resolve the filter the same way the collection policy does for a given map.
            var policy = new CollectionPolicy(collectionName, requireMapContext: false);
            var filter = ((IDynamicPolicy<Map, Thing>)policy).GetFilter(MakeUninitializedMap());
            var typedFilter = (IDynamicFilter<Map, Thing>)filter;

            // Assert
            Assert.True(typedFilter.Filter(wornApparel));
            Assert.False(typedFilter.Filter(healthyApparel));
            Assert.False(typedFilter.Filter(genericItem));
        }

        // ── Helpers ──

        private static Thing PushWornThing(bool isApparel, bool isWeapon, float hitPointPercentage)
        {
            var thing = MakeEquipmentThing(isApparel, isWeapon);
            var metadata = new IndexMetadata();
            metadata.Set(ToolkitConstants.Thing.HitPointPercentage, hitPointPercentage, persistent: true);
            Assert.True(Toolkit.Indexing.Manager.Push(thing, ref metadata, allowBuffering: false));
            return thing;
        }

        private static ThingWithComps MakeEquipmentThing(bool isApparel, bool isWeapon)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = "Test_WornEquipment";
            def.category = ThingCategory.Item;
            def.useHitPoints = true;
            if (isApparel)
            {
                def.apparel = new ApparelProperties();
            }
            if (isWeapon)
            {
                def.tools = new List<Tool> { new Tool() };
            }

            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            // Thing.Equals/GetHashCode are keyed on thingIDNumber and the database table is a dictionary keyed by
            // the thing, so each fixture needs a unique id.
            thing.thingIDNumber = _thingIdCounter++;
            return thing;
        }

        private static Map MakeUninitializedMap()
            => (Map)FormatterServices.GetUninitializedObject(typeof(Map));
    }
}
