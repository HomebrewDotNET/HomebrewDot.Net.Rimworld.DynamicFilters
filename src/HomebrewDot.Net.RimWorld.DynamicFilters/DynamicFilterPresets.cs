using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Contains the presets provided by the toolkit.
    /// </summary>
    public static class DynamicFilterPresets
    {
        /// <summary>
        /// All the preset policy names provided by the toolkit.
        /// </summary>
        private static readonly HashSet<string> AllPolicyNames = new HashSet<string>();
        private static Action<Action<string, IDynamicPolicyProvider>> Presets = (activator) => { };

        /// <summary>
        /// Policy name for the preset that contains all meat items.
        /// </summary>
        public const string MeatPreset = "Meat Preset";
        /// <summary>
        /// Policy name for the preset that contains all metal items.
        /// </summary>
        public const string MetalPreset = "Metal Preset";
        /// <summary>
        /// Policy name for the preset that contains all ingestible items.
        /// </summary>
        public const string IngestiblePreset = "Ingestible Preset";
        /// <summary>
        /// Policy name for the preset that contains all food items.
        /// </summary>
        public const string FoodPreset = "Food Preset";
        /// <summary>
        /// Policy name for the preset that contains all meal items.
        /// </summary>
        public const string MealPreset = "Meal Preset";
        /// <summary>
        /// Policy name for the preset that contains all good meal items.
        /// </summary>
        public const string GoodMealPreset = "Good Meal Preset";
        /// <summary>
        /// Policy name for the preset that contains all snack items. (Gives recreation or joy)
        /// </summary>
        public const string SnackPreset = "Snack Preset";
        /// <summary>
        /// Policy name for the preset that contains all medicinal items.
        /// </summary>
        public const string IsMedicinalPreset = "Medicinal Preset";
        /// <summary>
        /// Policy name for the preset that contains all apparel items.
        /// </summary>
        public const string IsApparelPreset = "Apparel Preset";
        /// <summary>
        /// Policy name for the preset that contains all weapon items.
        /// </summary>
        public const string IsWeaponPreset = "Weapon Preset";
        /// <summary>
        /// Policy name for the preset that contains all melee weapon items.
        /// </summary>
        public const string IsMeleeWeaponPreset = "Melee Weapon Preset";
        /// <summary>
        /// Policy name for the preset that contains all ranged weapon items.
        /// </summary>
        public const string IsRangedWeaponPreset = "Ranged Weapon Preset";
        /// <summary>
        /// Policy name for the preset that contains all flammable items.
        /// </summary>
        public const string FlammablePreset = "Flammable Preset";
        /// <summary>
        /// Policy name for the preset that contains all materials for everything that is currently buildable ny the player.
        /// </summary>
        public const string ConstructionPreset = "Construction Preset";
        /// <summary>
        /// Policy name for the preset that contains all things that can explode on death or when taking damage/being lit on fire.
        /// </summary>
        public const string ExplosivePreset = "Explosive Preset";

        /// <summary>
        /// Adds a preset provider to the toolkit. The provided action will be called with an activator that can be used to activate policies.
        /// Mainly used by patches.
        /// </summary>
        /// <param name="action">Delegate that will be called with another delegate for activating the preset</param>
        public static void AddPresetProvider(Action<Action<string, IDynamicPolicyProvider>> action)
        {
            lock(AllPolicyNames)
            {
                Presets += action;
            }
        }
        /// <summary>
        /// Enables all presets.
        /// </summary>
        public static void ActivatePresets()
        {
            Logging.Log("Activating all presets...");

            ActivateSimple(MeatPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeat).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(MetalPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMetal).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(IngestiblePreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsIngestible).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(FoodPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsNutritionGivingIngestible).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(MealPreset, CreatePropertyCondition($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                                                                 InOperatorType.DefaultTypeName, new FoodPreferability[]
                                                                 {
                                                                    FoodPreferability.MealTerrible,
                                                                    FoodPreferability.MealAwful,
                                                                    FoodPreferability.MealSimple,
                                                                    FoodPreferability.MealFine,
                                                                    FoodPreferability.MealLavish,
                                                                 }), true, false);
            ActivateSimple(GoodMealPreset, CreatePropertyCondition($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                                                                 InOperatorType.DefaultTypeName, new FoodPreferability[]
                                                                 {
                                                                    FoodPreferability.MealSimple,
                                                                    FoodPreferability.MealFine,
                                                                    FoodPreferability.MealLavish,
                                                                 }), true, false);
            ActivateSimple(SnackPreset, CreateSnackConditions(), true, false);
            ActivateSimple(IsMedicinalPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMedicine).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(IsApparelPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsApparel).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(IsWeaponPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(IsMeleeWeaponPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeleeWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(IsRangedWeaponPreset, CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsRangedWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            Toolkit.Indexing.Def.Thing.TrackIsConstructionMaterial();
            ActivateSimple(ConstructionPreset, CreatePropertyCondition(ToolkitConstants.Def.Thing.IsConstructionMaterial.Name, EqualsOperatorType.DefaultTypeName, true), true, false);
            ActivateSimple(ExplosivePreset, CreateExplosiveCondition(), true, false);
            ActivateSimple(FlammablePreset, CreateStatCondition(StatDefOf.Flammability, GreaterOperatorType.DefaultTypeName, 0), true, false);

            lock(AllPolicyNames)
            {
                Presets((policyName, provider) =>
                {
                    ActivatePolicy(policyName, provider);
                });
            }
        }
        /// <summary>
        /// Disables all presets.
        /// </summary>
        public static void DeactivatePresets()
        {
            Logging.Log("Deactivating all presets...");
            lock (AllPolicyNames)
            {
                foreach (var policyName in AllPolicyNames.ToArray())
                {
                    DynamicFiltersToolkit.Policies.DeactivateProvider(policyName);
                    AllPolicyNames.Remove(policyName);
                }
            }
        }

        /// <summary>
        /// Creates a condition for a property of a ThingDef.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="operator">The operator to use for the comparison.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreatePropertyCondition(string propertyName, string @operator, object value)
        {
            var conditionDef = ConditionBuilder.Build(builder =>
            builder.Compare.Indexed(propertyName)
                   .With.Operator(@operator)
                   .To.Value(value));

            return new SimpleFilterPolicyCondition[]
            {
                SimpleFilterPolicyCondition.FromDef(conditionDef)
            };
        }

        /// <summary>
        /// Creates a condition for a stat of a ThingDef.
        /// </summary>
        /// <param name="stat">The stat to create a condition for.</param>
        /// <param name="operator">The operator to use for the comparison.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateStatCondition(StatDef stat, string @operator, object value)
        {
            var conditionDef = ConditionBuilder.Build(builder =>
            builder.Compare.Stat(stat)
                   .With.Operator(@operator)
                   .To.Value(value));

            return new SimpleFilterPolicyCondition[]
            {
                SimpleFilterPolicyCondition.FromDef(conditionDef)
            };
        }

        /// <summary>
        /// Creates a condition for a component of a ThingDef.
        /// </summary>
        /// <param name="compType">The type of the component.</param>
        /// <param name="properties">The properties of the component to compare, can be null</param>
        /// <param name="operator">The operator to use for the comparison.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateCompCondition(Type compType, string properties, string @operator, object value)
        {
            var conditionDef = ConditionBuilder.Build(builder =>
            {
                if (string.IsNullOrWhiteSpace(properties))
                {
                    builder.Compare.Comp(compType)
                       .With.Operator(@operator)
                       .To.Value(value);
                }
                else
                {
                    builder.Compare.Comp($"{compType.FullName}{CompReferenceType.PathSeparator}{properties}")
                       .With.Operator(@operator)
                       .To.Value(value);
                }
            });

            return new SimpleFilterPolicyCondition[]
            {
                SimpleFilterPolicyCondition.FromDef(conditionDef)
            };
        }

        /// <summary>
        /// Creates a condition for a mod ID of a ThingDef.
        /// </summary>
        /// <param name="modIdRegex">The regular expression to match the mod ID.</param>
        /// <returns>A SimpleFilterPolicyCondition for the mod ID.</returns>
        public static SimpleFilterPolicyCondition CreateModFilterCondition(Regex modIdRegex)
        {
            modIdRegex = Guard.NotNull(modIdRegex, nameof(modIdRegex));

            Toolkit.Indexing.Thing.TrackModId();
            var conditionDef = ConditionBuilder.Build(builder =>
            {
                builder.Compare.Indexed(ToolkitConstants.Thing.ModId.Name)
                       .With.Match(modIdRegex);
            });

            var condition = SimpleFilterPolicyCondition.FromDef(conditionDef);
            condition.IsOr = false;
            return condition;
        }
        /// <summary>
        /// Creates a condition for a mod ID of a ThingDef.
        /// </summary>
        /// <param name="modId">The mod ID to match.</param>
        /// <returns>A SimpleFilterPolicyCondition for the mod ID.</returns>
        public static SimpleFilterPolicyCondition CreateModFilterCondition(string modId)
        {
            modId = Guard.NotNullOrWhitespace(modId, nameof(modId));

            Toolkit.Indexing.Thing.TrackModId();
            var conditionDef = ConditionBuilder.Build(builder =>
            {
                builder.Compare.Indexed(ToolkitConstants.Thing.ModId.Name)
                       .With.Equal()
                       .To.Value(modId);
            });

            var condition = SimpleFilterPolicyCondition.FromDef(conditionDef);
            condition.IsOr = false;
            return condition;
        }

        private static SimpleFilterPolicyCondition[] CreateExplosiveCondition()
        {
            var compExplosiveType = typeof(CompProperties_Explosive);
            var explodeOnDestoyed = nameof(CompProperties_Explosive.explodeOnDestroyed);
            var explodeOnDamageTaken = nameof(CompProperties_Explosive.startWickOnDamageTaken);
            var explodeOnDamageTakenHitPoints = nameof(CompProperties_Explosive.startWickHitPointsPercent);
            var listCount = nameof(List<DamageDef>.Count);

            var conditionDef = ConditionBuilder.Build(builder =>
                builder.Compare.Comp($"{compExplosiveType.FullName}{CompReferenceType.PathSeparator}{explodeOnDestoyed}")
                       .With.True()
                       .Or
                       .Compare.Comp($"{compExplosiveType.FullName}{CompReferenceType.PathSeparator}{explodeOnDamageTaken}.{listCount}")
                       .With.GreaterThan(0)
                       .Or
                       .Compare.Comp($"{compExplosiveType.FullName}{CompReferenceType.PathSeparator}{explodeOnDamageTakenHitPoints}")
                       .With.GreaterThan(0L)
            );

            return conditionDef.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray();
        }

        private static SimpleFilterPolicyCondition[] CreateSnackConditions()
        {
            var conditionDef = ConditionBuilder.Build(builder =>
                builder.Compare.Indexed($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}")
                       .With.Equal()
                       .To.Value(FoodPreferability.RawTasty)
                       .Or
                       .Compare.Indexed($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, float>(x => x.ingestible.joy).Name}")
                       .With.GreaterThan()
                       .To.Value(0)
            );
            return conditionDef.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray();
        }

        /// <summary>
        /// Activates a simple filter policy with the given name and conditions.
        /// </summary>
        /// <param name="policyName">The name of the policy to activate.</param>
        /// <param name="conditions">The conditions to apply to the policy.</param>
        /// <param name="thingDef">Whether the policy applies to ThingDefs.</param>
        /// <param name="disallowMatching">Whether to disallow matching items.</param>
        public static void ActivateSimple(string policyName, SimpleFilterPolicyCondition[] conditions, bool thingDef = true, bool disallowMatching = false)
        {
            var settings = new SimpleFilterPolicySettings()
            {
                Conditions = conditions.ToList(),
                ThingDef = thingDef,
                DisallowMatching = disallowMatching
            };
            var template = SimpleFilterPolicy.Instance;
            var provider = template.Create(settings);
            ActivatePolicy(policyName, provider);
        }

        /// <summary>
        /// Activates a preset with the given name and provider.
        /// </summary>
        /// <param name="policyName">The name of the policy to activate.</param>
        /// <param name="provider">The provider to use for the policy.</param>
        public static void ActivatePolicy(string policyName, IDynamicPolicyProvider provider)
        {
            DynamicFiltersToolkit.Policies.TryActivateProvider(policyName, provider, false, true);
            lock (AllPolicyNames)
            {
                AllPolicyNames.Add(policyName);
            }
        }
    }
}
