using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.IntegrationIndexing
{
    /// <summary>
    /// Validates the eager quality filter (CompQuality NotNull AND CompQuality|Quality &gt;= Normal) against a real
    /// snapshot, matching the user scenario where the autodexer has not (yet) written the comp into snapshot
    /// metadata. The getter must fall back to reading the comp from the thing itself when the
    /// <c>CompReferenceType:{CompQuality}</c> metadata key is absent, and use the metadata when present.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class QualityPresetEagerIntegrationTests : IDisposable
    {
        private static int _thingIdCounter = 1;
        private readonly List<string> _collectors = new List<string>();
        private readonly List<string> _activatedPolicies = new List<string>();

        public QualityPresetEagerIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            foreach (var name in _activatedPolicies)
            {
                InvokeSafe(() => DynamicFiltersToolkit.Policies.DeactivateProvider(name));
            }
            _activatedPolicies.Clear();
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
        public void QualityPreset_EagerCollection_CollectsNormalOrBetter_WithoutCompMetadata()
        {
            // Arrange: stand up the real indexing pipeline and push things carrying a CompQuality but no
            // CompReferenceType metadata (i.e. before the autodexer has run). The eager getter must fall back to
            // reading the comp directly from the thing.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var legendaryPants = PushQualityThing(QualityCategory.Legendary);
            var normalPants = PushQualityThing(QualityCategory.Normal);
            var poorPants = PushQualityThing(QualityCategory.Poor);
            var noQualityItem = PushNoQualityItem();

            var conditions = BuildQualityConditions(QualityCategory.Normal);
            var collectionName = $"TestQualityNoMeta_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(collectionName, x =>
            {
                foreach (var condition in conditions)
                {
                    _ = x.CompareFrom(condition.Condition);
                }
                return x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).GetSnapshot(), false);
            });
            _collectors.Add(collectionName);

            // Act
            var collector = (ICollector<Thing>)Toolkit.Collecting.GetAllCollectors()[collectionName];
            var collected = collector.GetAll().ToArray();

            // Assert: Normal and above match, Poor and no-quality do not.
            Assert.Contains(normalPants, collected);
            Assert.Contains(legendaryPants, collected);
            Assert.DoesNotContain(poorPants, collected);
            Assert.DoesNotContain(noQualityItem, collected);
        }

        [Fact]
        public void QualityPreset_EagerCollection_CollectsNormalOrBetter_WithCompMetadata()
        {
            // Arrange: same pipeline, but the snapshot rows carry the CompReferenceType metadata that the
            // autodexer writes in production. The eager getter must prefer the cached comp from metadata.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var normalPants = PushQualityThing(QualityCategory.Normal, withMetadata: true);
            var poorPants = PushQualityThing(QualityCategory.Poor, withMetadata: true);

            var conditions = BuildQualityConditions(QualityCategory.Normal);
            var collectionName = $"TestQualityWithMeta_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(collectionName, x =>
            {
                foreach (var condition in conditions)
                {
                    _ = x.CompareFrom(condition.Condition);
                }
                return x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).GetSnapshot(), false);
            });
            _collectors.Add(collectionName);

            // Act
            var collector = (ICollector<Thing>)Toolkit.Collecting.GetAllCollectors()[collectionName];
            var collected = collector.GetAll().ToArray();

            // Assert
            Assert.Contains(normalPants, collected);
            Assert.DoesNotContain(poorPants, collected);
        }

        [Fact]
        public void QualityPolicy_Activate_Eager_CollectsNormalOrBetter_ThroughFullActivationPath()
        {
            // Exercises the real SimpleFilterPolicy.Activate path (including the autodexer) for an eager
            // thing-level quality filter, matching the in-game scenario. The collector is registered but not
            // started in the test host (no Current.Game), so it is force-started exactly like the game does.
            Toolkit.Indexing.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);

            var legendaryPants = PushQualityThing(QualityCategory.Legendary);
            var normalPants = PushQualityThing(QualityCategory.Normal);
            var poorPants = PushQualityThing(QualityCategory.Poor);

            var policyName = $"TestQualityActivated_{Guid.NewGuid()}";
            var provider = QualityPolicy.Instance.Create(new QualityPolicySettings
            {
                MinimumQuality = QualityCategory.Normal,
                MaximumQuality = QualityCategory.Legendary
            });
            Assert.True(DynamicFiltersToolkit.Policies.TryActivateProvider(policyName, provider));
            _activatedPolicies.Add(policyName);

            Collecting.StartCollection();

            var collector = (ICollector<Thing>)Collecting.GetAllCollectors()[policyName];
            var collected = collector.GetAll().ToArray();

            Assert.Contains(normalPants, collected);
            Assert.Contains(legendaryPants, collected);
            Assert.DoesNotContain(poorPants, collected);
        }

        // ── Helpers ──

        private static List<SimpleFilterPolicyCondition> BuildQualityConditions(QualityCategory minimum)
        {
            var options = new QualityPolicySettings
            {
                MinimumQuality = minimum,
                MaximumQuality = QualityCategory.Legendary
            };
            var simple = Assert.IsAssignableFrom<SimpleFilterPolicySettings>(QualityPolicy.Instance.ConvertOptions(options));
            return simple.Conditions;
        }

        private static Thing PushQualityThing(QualityCategory quality, bool withMetadata = false)
        {
            var thing = MakeQualityThing(quality);
            var metadata = new IndexMetadata();
            if (withMetadata)
            {
                var comp = GetQualityComp(thing);
                metadata.Set(IndexMetadataKey.Get(CompMetadataKeyName), comp, persistent: true);
            }
            Assert.True(Toolkit.Indexing.Manager.Push(thing, ref metadata, allowBuffering: false));
            return thing;
        }

        private static Thing PushNoQualityItem()
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = "Test_NoQualityItem";
            def.category = ThingCategory.Item;

            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            thing.thingIDNumber = _thingIdCounter++;
            // Non-empty comps list with a non-quality comp (GetComp<T> fast path throws on an empty list).
            var rottable = (CompRottable)FormatterServices.GetUninitializedObject(typeof(CompRottable));
            SetComps(thing, new List<ThingComp> { rottable });

            var metadata = new IndexMetadata();
            Assert.True(Toolkit.Indexing.Manager.Push(thing, ref metadata, allowBuffering: false));
            return thing;
        }

        private static ThingWithComps MakeQualityThing(QualityCategory quality)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = "Test_QualityPants";
            def.category = ThingCategory.Item;

            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            thing.thingIDNumber = _thingIdCounter++;

            var comp = (CompQuality)FormatterServices.GetUninitializedObject(typeof(CompQuality));
            var qualityIntField = typeof(CompQuality).GetField("qualityInt", BindingFlags.Instance | BindingFlags.NonPublic);
            qualityIntField.SetValue(comp, quality);
            comp.parent = thing;
            SetComps(thing, new List<ThingComp> { comp });
            return thing;
        }

        private static void SetComps(ThingWithComps thing, List<ThingComp> comps)
        {
            var compsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);
            compsField.SetValue(thing, comps);
            var compsByTypeField = typeof(ThingWithComps).GetField("compsByType", BindingFlags.Instance | BindingFlags.NonPublic);
            var byType = comps
                .GroupBy(c => c.GetType())
                .ToDictionary(g => g.Key, g => g.ToArray());
            compsByTypeField.SetValue(thing, byType);
        }

        private static CompQuality GetQualityComp(Thing thing)
        {
            var compsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);
            var comps = (List<ThingComp>)compsField.GetValue(thing);
            return comps.OfType<CompQuality>().First();
        }

        private static string CompMetadataKeyName
            => $"CompReferenceType:{typeof(CompQuality).FullName}";
    }
}
