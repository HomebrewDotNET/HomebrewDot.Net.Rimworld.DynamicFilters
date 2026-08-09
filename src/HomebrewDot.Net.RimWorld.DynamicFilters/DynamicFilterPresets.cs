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
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Configuration.Components;
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
        private static Action<Action<string, string, IDynamicPolicyTemplate, IExposable>> Presets = (activator) => { };

        /// <summary>
        /// Policy name for the preset that contains all resource items.
        /// </summary>
        public const string ResourcePreset = "Resources";
        /// <summary>
        /// Policy name for the preset that contains all meat items.
        /// </summary>
        public const string MeatPreset = "Meats";
        /// <summary>
        /// Policy name for the preset that contains all metal items.
        /// </summary>
        public const string MetalPreset = "Metallic";
        /// <summary>
        /// Policy name for the preset that contains all wooden stuff.
        /// </summary>
        public const string WoodyPreset = "Wood";
        /// <summary>
        /// Policy name for the preset that contains all stony stuff.
        /// </summary>
        public const string StonyPreset = "Stony";
        /// <summary>
        /// Policy name for the preset that contains all fabric stuff.
        /// </summary>
        public const string FabricPreset = "Fabric";
        /// <summary>
        /// Policy name for the preset that contains all leather stuff.
        /// </summary>
        public const string LeatheryPreset = "Leather";
        /// <summary>
        /// Policy name for the preset that contains all plant matter.
        /// </summary>
        public const string PlantMatterPreset = "Plant Matter";
        /// <summary>
        /// Policy name for the preset that contains all ingestible items.
        /// </summary>
        public const string IngestiblePreset = "Ingestible";
        /// <summary>
        /// Policy name for the preset that contains all food items.
        /// </summary>
        public const string FoodPreset = "Food";
        /// <summary>
        /// Policy name for the preset that contains all meal items.
        /// </summary>
        public const string MealPreset = "Meals";
        /// <summary>
        /// Policy name for the preset that contains all good meal items.
        /// </summary>
        public const string GoodMealPreset = "Good Meals";
        /// <summary>
        /// Policy name for the preset that contains all snack items. (Gives recreation or joy)
        /// </summary>
        public const string SnackPreset = "Snacks";
        /// <summary>
        /// Policy name for the preset that contains all medicinal items.
        /// </summary>
        public const string IsMedicinalPreset = "Medicinal";
        /// <summary>
        /// Policy name for the preset that contains all apparel items.
        /// </summary>
        public const string IsApparelPreset = "Apparel";
        /// <summary>
        /// Policy name for the preset that contains all weapon items.
        /// </summary>
        public const string IsWeaponPreset = "Weapons";
        /// <summary>
        /// Policy name for the preset that contains all melee weapon items.
        /// </summary>
        public const string IsMeleeWeaponPreset = "Melee Weapons";
        /// <summary>
        /// Policy name for the preset that contains all ranged weapon items.
        /// </summary>
        public const string IsRangedWeaponPreset = "Ranged Weapons";
        /// <summary>
        /// Policy name for the preset that contains all flammable items.
        /// </summary>
        public const string FlammablePreset = "Flammable";
        /// <summary>
        /// Policy name for the preset that contains all materials for everything that is currently buildable ny the player.
        /// </summary>
        public const string ConstructionPreset = "Construction Materials";
        /// <summary>
        /// Policy name for the preset that contains all things that can explode on death or when taking damage/being lit on fire.
        /// </summary>
        public const string ExplosivesPreset = "Explosives";
        /// <summary>
        /// Policy name for the preset that contains all non-humanoid, non-mechanoid corpses suitable for butchering.
        /// </summary>
        public const string ButcheryCorpsePreset = "Butchery Corpses";
        /// <summary>
        /// Policy name for the preset that contains all humanoid corpses.
        /// </summary>
        public const string HumanoidCorpsePreset = "Humanoid Corpses";
        /// <summary>
        /// Policy name for the preset that contains all mechanoid corpses.
        /// </summary>
        public const string MechanoidCorpsePreset = "Mechanoid Corpses";
        /// <summary>
        /// Policy name for the preset that contains all foul meat.
        /// </summary>
        public const string FoulMeatPreset = "Foul Meat";
        /// <summary>
        /// Policy name for the preset that contains all foul leather.
        /// </summary>
        public const string FoulLeatherPreset = "Foul Leather";
        /// <summary>
        /// Policy name for the preset that contains all medical items.
        /// </summary>
        public const string IsMedicalPreset = "Medical Items";
        /// <summary>
        /// Policy name for the preset that contains all surgical parts (prosthetics, bionics, natural organs, etc.).
        /// </summary>
        public const string IsSurgicalPreset = "Surgical Parts";
        /// <summary>
        /// Policy name for the preset that contains all drinks.
        /// </summary>
        public const string DrinksPreset = "Drinks";
        /// <summary>
        /// Policy name for the preset that contains all non-alcoholic drinks.
        /// </summary>
        public const string NonAlcoholicDrinksPreset = "Non-Alcoholic Drinks";
        /// <summary>
        /// Policy name for the preset that contains all alcoholic drinks.
        /// </summary>
        public const string AlcoholicDrinksPreset = "Alcoholic Drinks";
        /// <summary>
        /// Policy name for the preset that contains coffee and tea.
        /// </summary>
        public const string CoffeeAndTeaPreset = "Coffee & Tea";
        /// <summary>
        /// Policy name for the preset that contains all perishable items (items with CompProperties_Rottable - needs freezer).
        /// </summary>
        public const string PerishesPreset = "Perishes";
        /// <summary>
        /// Policy name for the preset that contains all things that are currently rotting.
        /// </summary>
        public const string RottingPreset = "Rotting";
        /// <summary>
        /// Policy name for the preset that contains all smeltable items.
        /// </summary>
        public const string SmeltablePreset = "Smeltable";
        /// <summary>
        /// Policy name for the preset that contains all items that deteriorate when left outside.
        /// </summary>
        public const string DeterioratesPreset = "Deteriorates";
        /// <summary>
        /// Policy name for the preset that contains all biocoded items (bound to a specific pawn).
        /// </summary>
        public const string BiocodedPreset = "Biocoded";
        /// <summary>
        /// Policy name for the preset that contains all items with no quality.
        /// </summary>
        public const string NoQualityPreset = "No Quality";
        /// <summary>
        /// Policy name for the preset that contains all things whose tech level is above the tech level of the faction that owns the map.
        /// </summary>
        public const string AboveTechLevelPreset = "Above TechLevel";
        /// <summary>
        /// Policy name for the preset that contains all things whose tech level is below the tech level of the faction that owns the map.
        /// </summary>
        public const string BelowTechLevelPreset = "Below TechLevel";
        /// <summary>
        /// Adds a preset provider to the toolkit. The provided action will be called with an activator that can be used to activate policies.
        /// Mainly used by patches.
        /// </summary>
        /// <param name="action">Delegate that will be called with another delegate for activating the preset</param>
        public static void AddPresetProvider(Action<Action<string, string, IDynamicPolicyTemplate, IExposable>> action)
        {
            lock(Presets)
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

            CreateSimple(ResourcePreset, "Filters all resource defs", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.CountAsResource).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(MeatPreset, "Filters all meat defs", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeat).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(MetalPreset, "Filters all metallic defs", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMetal).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(WoodyPreset, "Filters all wooden stuff", BuildConditions(builder =>
                builder.Compare.Indexed(nameof(ThingDef.stuffCategories))
                       .With.Contains()
                       .To.StuffCategory("Woody")), true);
            CreateSimple(StonyPreset, "Filters all stony stuff", BuildConditions(builder =>
                builder.Compare.Indexed(nameof(ThingDef.stuffCategories))
                       .With.Contains()
                       .To.StuffCategory("Stony")), true);
            CreateSimple(FabricPreset, "Filters all fabric stuff", BuildConditions(builder =>
                builder.Compare.Indexed(nameof(ThingDef.stuffCategories))
                       .With.Contains()
                       .To.StuffCategory("Fabric")), true);
            CreateSimple(LeatheryPreset, "Filters all leather stuff", BuildConditions(builder =>
                builder.Compare.Indexed(nameof(ThingDef.stuffCategories))
                       .With.Contains()
                       .To.StuffCategory("Leathery")), true);
            CreateSimple(PlantMatterPreset, "Filters all plant matter", BuildConditions(builder =>
                builder.Compare.Self()
                       .With.InThingCategory("PlantMatter")), true);
            CreateSimple(IngestiblePreset, "Filters all defs that can be ingested", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsIngestible).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(FoodPreset, "Filters all defs that can be ingested and provides nutrition", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsNutritionGivingIngestible).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(MealPreset, "Filters all meal defs", CreatePropertyCondition($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                                                                 InOperatorType.DefaultTypeName, new FoodPreferability[]
                                                                 {
                                                                    FoodPreferability.MealTerrible,
                                                                    FoodPreferability.MealAwful,
                                                                    FoodPreferability.MealSimple,
                                                                    FoodPreferability.MealFine,
                                                                    FoodPreferability.MealLavish,
                                                                 }), true);
            CreateSimple(GoodMealPreset, "Filters all meal defs that don't taste awful", CreatePropertyCondition($"{Toolkit.Helpers.Expression.GetMember<ThingDef, IngestibleProperties>(x => x.ingestible).Name}.{Toolkit.Helpers.Expression.GetMember<ThingDef, FoodPreferability>(x => x.ingestible.preferability).Name}",
                                                                 InOperatorType.DefaultTypeName, new FoodPreferability[]
                                                                 {
                                                                    FoodPreferability.MealSimple,
                                                                    FoodPreferability.MealFine,
                                                                    FoodPreferability.MealLavish,
                                                                 }), true);
            CreateSimple(SnackPreset, "Filters all defs that are tasty raw or give joy when ingested", CreateSnackConditions(), true);
            CreateSimple(IsMedicinalPreset, "Filters all defs that are medicinal", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMedicine).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(IsApparelPreset, "Filters all defs that are apparel", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsApparel).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(IsWeaponPreset, "Filters all defs that are weapons", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(IsMeleeWeaponPreset, "Filters all defs that are melee weapons", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeleeWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(IsRangedWeaponPreset, "Filters all defs that are ranged weapons", CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsRangedWeapon).Name, EqualsOperatorType.DefaultTypeName, true), true);
            Toolkit.Indexing.Def.Thing.TrackIsConstructionMaterial();
            CreateSimple(ConstructionPreset, "Filters all defs that are currently usable to build stuff. Updated when research is completed", CreatePropertyCondition(ToolkitConstants.Def.Thing.IsConstructionMaterial.Name, EqualsOperatorType.DefaultTypeName, true), true);
            CreateSimple(ExplosivesPreset, "Filters all defs that could explode when hit", CreateExplosiveCondition(), true);
            CreateSimple(FlammablePreset, "Filters all defs that are flammable", CreateStatCondition(StatDefOf.Flammability, GreaterOperatorType.DefaultTypeName, 0), true);
            CreateSimple(ButcheryCorpsePreset, "Filters all non-humanoid, non-mechanoid corpses for butchering",
                ConditionBuilder.Build(builder =>
                {
                    var condition = builder.Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsCorpse).Name)
                                           .With.True()
                                           .And
                                           .Compare.Indexed($"{nameof(ThingDef.race)}.{nameof(RaceProperties.Humanlike)}")
                                           .With.False()
                                           .And
                                           .Compare.Indexed($"{nameof(ThingDef.race)}.{nameof(RaceProperties.IsMechanoid)}")
                                           .With.False();
                    // Big and Small - Framework adds robot corpses under BS_RobotCorpses. They are non-humanoid,
                    // non-mechanoid and would otherwise be treated as butcherable, so exclude them.
                    if (ToolkitConstants.Mods.BigAndSmall.IsLoaded)
                    {
                        condition = condition.And
                                             .Compare.Self()
                                             .With.InThingCategory(ToolkitConstants.Mods.BigAndSmall.RobotCorpseCategoryDefName)
                                             .Not();
                    }
                    // Vanilla Quests Expanded - Drone Factory adds drone corpses. They are non-humanoid,
                    // non-mechanoid and would otherwise be treated as butcherable, so exclude them.
                    // With Odyssey loaded the mod patches the VQE_Drone flesh type's corpseCategory to
                    // Odyssey's CorpsesDrone, which empties VQE_CorpsesDrone, so only check it without Odyssey.
                    if (ToolkitConstants.Mods.VqeDroneFactory.IsLoaded && !ToolkitConstants.Odyssey.IsLoaded)
                    {
                        condition = condition.And
                                             .Compare.Self()
                                             .With.InThingCategory(ToolkitConstants.Mods.VqeDroneFactory.DroneCorpseCategoryDefName)
                                             .Not();
                    }
                    // The Odyssey expansion defines CorpsesDrone, and the drone factory mod re-targets its drone
                    // corpses there via a patch when Odyssey is active. Odyssey's own drone corpses use it too,
                    // so exclude it whenever Odyssey is loaded.
                    if (ToolkitConstants.Odyssey.IsLoaded)
                    {
                        condition = condition.And
                                             .Compare.Self()
                                             .With.InThingCategory(ToolkitConstants.Odyssey.DroneCorpseCategoryDefName)
                                             .Not();
                    }
                }).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                true);
            CreateSimple(HumanoidCorpsePreset, "Filters all humanoid corpses",
                ConditionBuilder.Build(builder =>
                    builder.Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsCorpse).Name)
                           .With.True()
                           .And
                           .Compare.Indexed($"{nameof(ThingDef.race)}.{nameof(RaceProperties.Humanlike)}")
                           .With.True()
                ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                true);
            CreateSimple(MechanoidCorpsePreset, "Filters all mechanoid corpses",
                ConditionBuilder.Build(builder =>
                    builder.Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsCorpse).Name)
                           .With.True()
                           .And
                           .Compare.Indexed($"{nameof(ThingDef.race)}.{nameof(RaceProperties.IsMechanoid)}")
                           .With.True()
                ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                true);
            Toolkit.Indexing.Def.Thing.TrackIsFoul();
            CreateSimple(FoulMeatPreset, "Filters all foul meat (human, insect, twisted, etc.)",
                BuildConditions(builder =>
                {
                    // The Bad Meat Category mod moves defs out of MeatRaw (so IsMeat becomes false for them),
                    // hence the top-level OR: (isFoul AND isMeat) OR (in MeatBad).
                    var foulMeat = builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsFoul.Name)
                                          .With.True()
                                          .And
                                          .Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsMeat).Name)
                                          .With.True();
                    if (ToolkitConstants.Mods.BadMeatCategory.IsLoaded)
                    {
                        _ = foulMeat.Or
                                   .Compare.Self()
                                   .With.InThingCategory(ToolkitConstants.Mods.BadMeatCategory.MeatBadCategoryDefName);
                    }
                }),
                true);
            CreateSimple(FoulLeatherPreset, "Filters all foul leather (human, insect, etc.)",
                BuildConditions(builder =>
                {
                    // The Bad Leather Category mod moves defs out of Leathers (so IsLeather becomes false for them),
                    // hence the top-level OR: (isFoul AND isLeather) OR (in LeatherBad).
                    var foulLeather = builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsFoul.Name)
                                            .With.True()
                                            .And
                                            .Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, bool>(x => x.IsLeather).Name)
                                            .With.True();
                    if (ToolkitConstants.Mods.BadLeatherCategory.IsLoaded)
                    {
                        _ = foulLeather.Or
                                      .Compare.Self()
                                      .With.InThingCategory(ToolkitConstants.Mods.BadLeatherCategory.LeatherBadCategoryDefName);
                    }
                }),
                true);
            Toolkit.Indexing.Def.Thing.TrackIsDrink();
            Toolkit.Indexing.Def.Thing.TrackIsAlcoholic();
            CreateSimple(DrinksPreset, "Filters all drinks (beer, tea, juices, soda, etc.)",
                BuildConditions(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsDrink.Name)
                           .With.True()
                ),
                true);
            CreateSimple(AlcoholicDrinksPreset, "Filters all alcoholic drinks",
                BuildConditions(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsAlcoholic.Name)
                           .With.True()
                ),
                true);
            CreateSimple(NonAlcoholicDrinksPreset, "Filters all non-alcoholic drinks",
                ConditionBuilder.Build(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsDrink.Name)
                           .With.True()
                           .And
                           .Compare.Indexed(ToolkitConstants.Def.Thing.IsAlcoholic.Name)
                           .With.False()
                ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                true);
            CreateSimple(CoffeeAndTeaPreset, "Filters coffee and tea drinks",
                ConditionBuilder.Build(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsDrink.Name)
                           .With.True()
                           .And
                           .Compare.Indexed(nameof(ThingDef.defName))
                           .With.Match(new System.Text.RegularExpressions.Regex("(?i)(coffee|tea)", RegexOptions.Compiled))
                ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                true);
            Toolkit.Indexing.Def.Thing.TrackIsMedical();
            CreateSimple(IsMedicalPreset, "Filters all medical items (medicine, medical drugs, etc.)",
                BuildConditions(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsMedical.Name)
                           .With.True()
                ),
                true);
            Toolkit.Indexing.Def.Thing.TrackIsSurgical();
            CreateSimple(IsSurgicalPreset, "Filters all surgical parts (prosthetics, bionics, natural organs, etc.)",
                BuildConditions(builder =>
                    builder.Compare.Indexed(ToolkitConstants.Def.Thing.IsSurgical.Name)
                           .With.True()
                ),
                true);
            CreateSimple(PerishesPreset, "Filters all perishable items (items that rot and need a freezer)",
                BuildConditions(builder =>
                    builder.Compare.Comp(typeof(CompProperties_Rottable))
                           .With.NotNull()
                ),
                true);
            CreateSimple(RottingPreset, "Filters all things that are currently rotting",
                BuildConditions(builder =>
                    builder.Compare.Comp($"{typeof(CompRottable).FullName}{CompReferenceType.PathSeparator}{nameof(CompRottable.Stage)}")
                           .With.Equal()
                           .To.Value(RotStage.Rotting)
                ),
                false);
            CreateSimple(SmeltablePreset, "Filters all things that can be smelted at a smelter (matches vanilla Allow Smeltable)",
                CreateSmeltableCondition(),
                false);
            CreateSimple(DeterioratesPreset, "Filters all items that deteriorate when left outside",
                CreateStatCondition(StatDefOf.DeteriorationRate, GreaterOperatorType.DefaultTypeName, 0),
                true);
            CreateSimple(BiocodedPreset, "Filters all biocoded items (bound to a specific pawn)",
                BuildConditions(builder =>
                    builder.Compare.Comp($"{typeof(CompBiocodable).FullName}{CompReferenceType.PathSeparator}{nameof(CompBiocodable.Biocoded)}")
                           .With.True()
                ),
                false);
            CreateSimple(NoQualityPreset, "Filters all items with no quality (raw resources, components, etc.)",
                BuildConditions(builder =>
                    builder.Compare.Comp(typeof(CompQuality))
                           .With.Null()
                ),
                false);
            CreateSimple(AboveTechLevelPreset, "Filters all things whose tech level is above the tech level of the faction that owns the map",
                CreateTechLevelCondition(GreaterOperatorType.DefaultTypeName),
                false);
            CreateSimple(BelowTechLevelPreset, "Filters all things whose tech level is below the tech level of the faction that owns the map",
                CreateTechLevelCondition(LesserOperatorType.DefaultTypeName),
                false);
            Presets((name, description, template, settings) =>
            {
                CreatePreset(name, description, template, settings);
            });
        }

        /// <summary>
        /// Builds conditions from a builder action, handling both single and multi-condition results correctly.
        /// </summary>
        private static SimpleFilterPolicyCondition[] BuildConditions(Action<IConditionBuilder> buildAction)
        {
            var def = ConditionBuilder.Build(buildAction);
            if (def.Conditions != null && def.Conditions.Length > 0)
            {
                return def.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray();
            }
            return new[] { SimpleFilterPolicyCondition.FromDef(def) };
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
        /// Creates a condition comparing the tech level of a thing's def against the tech level of the faction that owns the map the thing is on.
        /// Things without a tech level (TechLevel.Undefined) are excluded, as are things on maps without a parent faction.
        /// Collections evaluate <see cref="IIndexed{Thing}"/> entries, so both operands must use the Indexed reference type: the first path segment resolves from the indexed value's member or metadata, and the remainder is traversed on the resolved object.
        /// </summary>
        /// <param name="operator">The operator to use for the comparison (e.g. GreaterThan for above, LessThan for below).</param>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateTechLevelCondition(string @operator)
        {
            return BuildConditions(builder =>
                builder.Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.techLevel)}")
                       .With.NotEqual()
                       .To.Value(TechLevel.Undefined)
                       .And
                       .Compare.Indexed($"{nameof(Thing.Map)}.{nameof(Map.ParentFaction)}")
                       .With.NotNull()
                       .And
                       .Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.techLevel)}")
                       .With.Operator(@operator)
                       .To.Indexed($"{nameof(Thing.Map)}.{nameof(Map.ParentFaction)}.{nameof(Faction.def)}.{nameof(FactionDef.techLevel)}"));
        }

        /// <summary>
        /// Creates thing-level conditions that mirror the game's per-instance smeltability check, i.e. the same
        /// logic as <see cref="Thing.Smeltable"/> and the vanilla "Allow Smeltable" special thing filter. A thing is
        /// smeltable when its def is marked <see cref="ThingDef.smeltable"/> and, when the def is made from stuff, the
        /// stuff is smeltable too (e.g. a steel club is smeltable, a wooden club is not). To match non-smeltable
        /// things, invert this condition (Not) in the filter settings.
        /// Collections evaluate <see cref="IIndexed{T}"/> entries, so both operands use the Indexed reference type:
        /// the first path segment resolves from the indexed value's member or metadata, and the remainder is
        /// traversed on the resolved object.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateSmeltableCondition()
        {
            var defSmeltable = $"{nameof(Thing.def)}.{nameof(ThingDef.smeltable)}";
            var madeFromStuff = $"{nameof(Thing.def)}.{nameof(ThingDef.MadeFromStuff)}";
            var stuffSmeltable = $"{nameof(Thing.Stuff)}.{nameof(ThingDef.smeltable)}";

            return BuildConditions(builder =>
            {
                // def.smeltable AND (not made from stuff OR stuff is smeltable)
                builder.Compare.Indexed(defSmeltable)
                       .With.True()
                       .And
                       .Group(stuff => stuff
                           .Compare.Indexed(madeFromStuff)
                           .With.False()
                           .Or
                           .Compare.Indexed(stuffSmeltable)
                           .With.True());
            });
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
        public static void CreateSimple(string presetName, string description, SimpleFilterPolicyCondition[] conditions, bool thingDef = true)
        {
            var settings = new SimpleFilterPolicySettings()
            {
                Conditions = conditions.ToList(),
                ThingDef = thingDef
            };
            var template = SimpleFilterPolicy.Instance;
            CreatePreset<SimpleFilterPolicy>(presetName, description, template, settings);
        }

        /// <summary>
        /// Activates a preset with the given name and provider.
        /// </summary>
        /// <param name="policyName">The name of the policy to activate.</param>
        /// <param name="provider">The provider to use for the policy.</param>
        public static void CreatePreset<T>(string presetName, string description, T policy, IExposable settings) where T : IDynamicPolicyTemplate
        {
            var preset = new DelegatedPolicyPreset<T>(presetName, description, policy, settings);
            DynamicFiltersToolkit.Templates.AddTemplate(preset);
        }
    }
}
