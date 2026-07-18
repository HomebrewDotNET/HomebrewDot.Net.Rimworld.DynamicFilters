using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Testing.Models;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Indexing;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.RimWorld.DynamicFilters.Tests.DynamicFilterPresetsFixture;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class DynamicFilterPresetsIntegrationTests : IClassFixture<DynamicFilterPresetsFixture>, IDisposable
    {
        private readonly DynamicFilterPresetsFixture _fixture;
        private readonly List<string> _activatedPolicies = new List<string>();

        public DynamicFilterPresetsIntegrationTests(DynamicFilterPresetsFixture fixture)
        {
            _fixture = fixture;
        }

        public void Dispose()
        {
            foreach (var policyName in _activatedPolicies)
            {
                InvokeSafe(() => DynamicFiltersToolkit.Policies.DeactivateProvider(policyName));
            }
            _activatedPolicies.Clear();
        }

        private void ActivateAndTrack(string policyName, SimpleFilterPolicyCondition[] conditions)
        {
            var settings = new SimpleFilterPolicySettings
            {
                Conditions = conditions.ToList(),
                ThingDef = true
            };
            DynamicFiltersToolkit.Policies.TryActivateProvider(policyName, SimpleFilterPolicy.Instance.Create(settings));
            _activatedPolicies.Add(policyName);
        }

        private static ICollector<ThingDef> GetCollector(string policyName)
        {
            Collecting.GetAllCollectors().TryGetValue(policyName, out var collector);
            return collector as ICollector<ThingDef>;
        }

        private static string[] GetCollectedDefNames(ICollector<ThingDef> collector)
        {
            return collector?.GetAll().Select(d => d.defName).ToArray() ?? Array.Empty<string>();
        }

        /// <summary>
        /// Builds a collector on a custom Tentity table with the given conditions, registers it as a policy,
        /// and returns the collector for verification.
        /// </summary>
        private ICollector<Tentity<T>> BuildTentityCollector<T>(string policyName, string tableName,
            SimpleFilterPolicyCondition[] conditions) where T : struct
        {
            Collecting.Build(policyName, x =>
            {
                foreach (var condition in conditions)
                    _ = x.CompareFrom(condition.Condition);
                return x.CollectFromSnapshot<ICollectionBuilder, Tentity<T>>(
                    d => d.GetTable<Tentity<T>>(tableName),
                    d => d.GetTable<Tentity<T>>(tableName)?.Enumerate<IIndexed<Tentity<T>>>()
                        ?? Enumerable.Empty<IIndexed<Tentity<T>>>());
            });
            _activatedPolicies.Add(policyName);
            Collecting.GetAllCollectors().TryGetValue(policyName, out var collector);
            return collector as ICollector<Tentity<T>>;
        }

        // ═══════════════════════════════════
        // Meat Preset (via Tentity<bool> mock)
        // ═══════════════════════════════════
        [Fact]
        public void MeatPreset_MatchesMeatDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<bool>.Boolean),
                EqualsOperatorType.DefaultTypeName, true);
            var collector = BuildTentityCollector<bool>(DynamicFilterPresets.MeatPreset, "TentityBool", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void MeatPreset_DoesNotMatchNonMeatDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeat).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.MeatPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.MeatPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(MetalDefName, defNames);
        }

        // ═══════════════════════════════════
        // Metal Preset (via Tentity<bool> mock)
        // ═══════════════════════════════════
        [Fact]
        public void MetalPreset_MatchesMetalDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<bool>.Boolean),
                EqualsOperatorType.DefaultTypeName, true);
            var collector = BuildTentityCollector<bool>(DynamicFilterPresets.MetalPreset, "TentityBool", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void MetalPreset_DoesNotMatchNonMetalDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMetal).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.MetalPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.MetalPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(MeatDefName, defNames);
        }

        // ═══════════════════════════════════
        // Ingestible Preset
        // ═══════════════════════════════════
        [Fact]
        public void IngestiblePreset_MatchesIngestibleDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsIngestible).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IngestiblePreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IngestiblePreset));
            Assert.Contains(IngestibleDefName, defNames);
            Assert.Contains(FoodDefName, defNames);
        }

        [Fact]
        public void IngestiblePreset_DoesNotMatchNonIngestibleDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsIngestible).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IngestiblePreset, conditions);
            Assert.DoesNotContain(GenericItemDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.IngestiblePreset)));
        }

        // ═══════════════════════════════════
        // Food Preset (via Tentity<bool> mock)
        // ═══════════════════════════════════
        [Fact]
        public void FoodPreset_MatchesFoodDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<bool>.Boolean),
                EqualsOperatorType.DefaultTypeName, true);
            var collector = BuildTentityCollector<bool>(DynamicFilterPresets.FoodPreset, "TentityBool", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void FoodPreset_DoesNotMatchNonFoodIngestible()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsNutritionGivingIngestible).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.FoodPreset, conditions);
            Assert.DoesNotContain(GenericItemDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.FoodPreset)));
        }

        // ═══════════════════════════════════
        // Meal Preset
        // ═══════════════════════════════════
        [Fact]
        public void MealPreset_MatchesMealDefs()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                $"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                InOperatorType.DefaultTypeName,
                new[] { FoodPreferability.MealTerrible, FoodPreferability.MealAwful, FoodPreferability.MealSimple, FoodPreferability.MealFine, FoodPreferability.MealLavish });
            ActivateAndTrack(DynamicFilterPresets.MealPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.MealPreset));
            Assert.Contains(MealSimpleDefName, defNames);
            Assert.Contains(MealLavishDefName, defNames);
            Assert.Contains(MealAwfulDefName, defNames);
        }

        [Fact]
        public void MealPreset_DoesNotMatchNonMealDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                $"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                InOperatorType.DefaultTypeName,
                new[] { FoodPreferability.MealTerrible, FoodPreferability.MealAwful, FoodPreferability.MealSimple, FoodPreferability.MealFine, FoodPreferability.MealLavish });
            ActivateAndTrack(DynamicFilterPresets.MealPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.MealPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(FoodDefName, defNames);
        }

        // ═══════════════════════════════════
        // Good Meal Preset
        // ═══════════════════════════════════
        [Fact]
        public void GoodMealPreset_MatchesGoodMealDefs()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                $"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                InOperatorType.DefaultTypeName,
                new[] { FoodPreferability.MealSimple, FoodPreferability.MealFine, FoodPreferability.MealLavish });
            ActivateAndTrack(DynamicFilterPresets.GoodMealPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.GoodMealPreset));
            Assert.Contains(MealSimpleDefName, defNames);
            Assert.Contains(MealLavishDefName, defNames);
        }

        [Fact]
        public void GoodMealPreset_DoesNotMatchAwfulMeal()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                $"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                InOperatorType.DefaultTypeName,
                new[] { FoodPreferability.MealSimple, FoodPreferability.MealFine, FoodPreferability.MealLavish });
            ActivateAndTrack(DynamicFilterPresets.GoodMealPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.GoodMealPreset));
            Assert.DoesNotContain(MealAwfulDefName, defNames);
            Assert.DoesNotContain(GenericItemDefName, defNames);
        }

        // ═══════════════════════════════════
        // Snack Preset
        // ═══════════════════════════════════
        [Fact]
        public void SnackPreset_MatchesSnackViaJoy()
        {
            var conditions = CreateSnackConditions();
            ActivateAndTrack(DynamicFilterPresets.SnackPreset, conditions);
            Assert.Contains(SnackJoyDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.SnackPreset)));
        }

        [Fact]
        public void SnackPreset_MatchesSnackViaRawTasty()
        {
            var conditions = CreateSnackConditions();
            ActivateAndTrack(DynamicFilterPresets.SnackPreset, conditions);
            Assert.Contains(SnackRawTastyDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.SnackPreset)));
        }

        [Fact]
        public void SnackPreset_DoesNotMatchRegularFood()
        {
            var conditions = CreateSnackConditions();
            ActivateAndTrack(DynamicFilterPresets.SnackPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.SnackPreset));
            Assert.DoesNotContain(FoodDefName, defNames);
            Assert.DoesNotContain(GenericItemDefName, defNames);
        }

        private static SimpleFilterPolicyCondition[] CreateSnackConditions()
        {
            var conditionDef = ConditionBuilder.Build(builder =>
                builder.Compare
                    .Indexed($"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}")
                    .With.Equal()
                    .To.Value(FoodPreferability.RawTasty)
                    .Or
                    .Compare
                    .Indexed($"{Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Helpers.Expression.GetMember<ThingDef, float>(x => x.ingestible.joy).Name}")
                    .With.GreaterThan()
                    .To.Value(0));
            return conditionDef.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray();
        }

        // ═══════════════════════════════════
        // Medicinal Preset (via Tentity<bool> mock)
        // ═══════════════════════════════════
        [Fact]
        public void MedicinalPreset_MatchesMedicineDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<bool>.Boolean),
                EqualsOperatorType.DefaultTypeName, true);
            var collector = BuildTentityCollector<bool>(DynamicFilterPresets.IsMedicinalPreset, "TentityBool", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void MedicinalPreset_DoesNotMatchNonMedicineDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMedicine).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsMedicinalPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsMedicinalPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(FoodDefName, defNames);
        }

        // ═══════════════════════════════════
        // Apparel Preset
        // ═══════════════════════════════════
        [Fact]
        public void ApparelPreset_MatchesApparelDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsApparel).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsApparelPreset, conditions);
            Assert.Contains(ApparelDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsApparelPreset)));
        }

        [Fact]
        public void ApparelPreset_DoesNotMatchNonApparelDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsApparel).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsApparelPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsApparelPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(MeleeWeaponDefName, defNames);
        }

        // ═══════════════════════════════════
        // Weapon Preset
        // ═══════════════════════════════════
        [Fact]
        public void WeaponPreset_MatchesWeaponDefs()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsWeaponPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsWeaponPreset));
            Assert.Contains(MeleeWeaponDefName, defNames);
            Assert.Contains(RangedWeaponDefName, defNames);
        }

        [Fact]
        public void WeaponPreset_DoesNotMatchNonWeaponDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsWeaponPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsWeaponPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(ApparelDefName, defNames);
        }

        // ═══════════════════════════════════
        // Melee Weapon Preset
        // ═══════════════════════════════════
        [Fact]
        public void MeleeWeaponPreset_MatchesMeleeWeaponDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeleeWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsMeleeWeaponPreset, conditions);
            Assert.Contains(MeleeWeaponDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsMeleeWeaponPreset)));
        }

        [Fact]
        public void MeleeWeaponPreset_DoesNotMatchRangedWeapon()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeleeWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsMeleeWeaponPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsMeleeWeaponPreset));
            Assert.DoesNotContain(RangedWeaponDefName, defNames);
            Assert.DoesNotContain(GenericItemDefName, defNames);
        }

        // ═══════════════════════════════════
        // Ranged Weapon Preset
        // ═══════════════════════════════════
        [Fact]
        public void RangedWeaponPreset_MatchesRangedWeaponDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsRangedWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsRangedWeaponPreset, conditions);
            Assert.Contains(RangedWeaponDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsRangedWeaponPreset)));
        }

        [Fact]
        public void RangedWeaponPreset_DoesNotMatchMeleeWeapon()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsRangedWeapon).Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.IsRangedWeaponPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.IsRangedWeaponPreset));
            Assert.DoesNotContain(MeleeWeaponDefName, defNames);
            Assert.DoesNotContain(GenericItemDefName, defNames);
        }

        // ═══════════════════════════════════
        // Flammable Preset (via Tentity<float> mock)
        // ═══════════════════════════════════
        [Fact]
        public void FlammablePreset_MatchesFlammableDef()
        {
            // Original uses stat condition (needs Unity's StatWorker). 
            // Tentity equivalent: indexed property condition on floatingNumber > 0.
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<float>.FloatingNumber),
                GreaterOperatorType.DefaultTypeName, 0);
            var collector = BuildTentityCollector<float>(DynamicFilterPresets.FlammablePreset, "TentityFloat", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void FlammablePreset_DoesNotMatchNonFlammableDef()
        {
            var conditions = DynamicFilterPresets.CreateStatCondition(
                FlammabilityDef, GreaterOperatorType.DefaultTypeName, 0);
            ActivateAndTrack(DynamicFilterPresets.FlammablePreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.FlammablePreset));
            Assert.DoesNotContain(NonFlammableDefName, defNames);
        }

        // ═══════════════════════════════════
        // Construction Preset
        // ═══════════════════════════════════
        [Fact]
        public void ConstructionPreset_MatchesConstructionDef()
        {
            // IsConstructionMaterial is overridden via metadata in SetupIndexing
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                ToolkitConstants.Def.Thing.IsConstructionMaterial.Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.ConstructionPreset, conditions);
            Assert.Contains(ConstructionDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.ConstructionPreset)));
        }

        [Fact]
        public void ConstructionPreset_DoesNotMatchNonConstructionDef()
        {
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                ToolkitConstants.Def.Thing.IsConstructionMaterial.Name,
                EqualsOperatorType.DefaultTypeName, true);
            ActivateAndTrack(DynamicFilterPresets.ConstructionPreset, conditions);
            Assert.DoesNotContain(GenericItemDefName, GetCollectedDefNames(GetCollector(DynamicFilterPresets.ConstructionPreset)));
        }

        // ═══════════════════════════════════
        // Explosive Preset (via Tentity<bool> mock)
        // ═══════════════════════════════════
        [Fact]
        public void ExplosivePreset_MatchesExplosiveDef()
        {
            // Original uses comp condition (needs Unity type resolution).
            // Tentity equivalent: indexed property condition on boolean == true.
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                nameof(Tentity<bool>.Boolean),
                EqualsOperatorType.DefaultTypeName, true);
            var collector = BuildTentityCollector<bool>(DynamicFilterPresets.ExplosivesPreset, "TentityBool", conditions);
            var items = collector?.GetAll().Select(i => i.text).ToArray() ?? Array.Empty<string>();
            Assert.Contains(TentityMatchingText, items);
        }

        [Fact]
        public void ExplosivePreset_DoesNotMatchNonExplosiveDef()
        {
            var conditions = CreateExplosiveConditions();
            ActivateAndTrack(DynamicFilterPresets.ExplosivesPreset, conditions);
            var defNames = GetCollectedDefNames(GetCollector(DynamicFilterPresets.ExplosivesPreset));
            Assert.DoesNotContain(GenericItemDefName, defNames);
            Assert.DoesNotContain(FoodDefName, defNames);
        }

        private static SimpleFilterPolicyCondition[] CreateExplosiveConditions()
        {
            var compExplosiveType = typeof(CompProperties_Explosive);
            var explodeOnDestroyed = nameof(CompProperties_Explosive.explodeOnDestroyed);
            var explodeOnDamageTaken = nameof(CompProperties_Explosive.startWickOnDamageTaken);
            var listCount = nameof(List<DamageDef>.Count);
            var conditionDef = ConditionBuilder.Build(builder =>
                builder.Compare
                    .Comp($"{compExplosiveType.FullName}{CompReferenceType.PathSeparator}{explodeOnDestroyed}")
                    .With.Operator(TrueOperatorType.DefaultTypeName)
                    .Or
                    .Compare
                    .Comp($"{compExplosiveType.FullName}{CompReferenceType.PathSeparator}{explodeOnDamageTaken}.{listCount}")
                    .With.Operator(GreaterOperatorType.DefaultTypeName)
                    .To.Value(0));
            return conditionDef.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray();
        }
    }
}