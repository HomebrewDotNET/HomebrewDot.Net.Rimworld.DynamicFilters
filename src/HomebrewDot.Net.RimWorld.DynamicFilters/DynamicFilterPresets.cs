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
using HomebrewDot.Net.Rimworld.Policies.Templates;
using HomebrewDot.Net.Rimworld.Referencing;
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
        /// Whether the special thing filter presets have been created in this session. Prevents duplicates when
        /// preset activation runs more than once (e.g. toggling settings within the same session).
        /// </summary>
        private static bool _specialThingFilterPresetsActivated;

        /// <summary>
        /// The preset kind used for the special thing filter presets, which is also the UI prefix.
        /// </summary>
        public const string SpecialThingFilterPresetKind = "ThingFilter";

        /// <summary>
        /// DefNames of special thing filters that duplicate a built-in condition preset. While special thing filter
        /// presets are enabled, the special thing filter preset wins and the built-in preset is skipped.
        /// </summary>
        private static readonly HashSet<string> DuplicateSpecialThingFilterDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Duplicates RottingPreset (thing comp CompRottable.Stage in {Rotting, Dessicated}).
            "AllowRotten",
            // Duplicates ColonistCorpsePreset / StrangerCorpsePreset / SlaveCorpsePreset / UnnaturalCorpsePreset.
            "AllowCorpsesColonist",
            "AllowCorpsesStranger",
            "AllowCorpsesSlave",
            "AllowCorpsesUnnatural",
            // Duplicates SmeltablePreset (mirrors Thing.Smeltable, i.e. the vanilla Allow Smeltable check).
            "AllowSmeltable",
            "AllowSmeltableApparel",
            // Duplicates BiocodedPreset (comp CompBiocodable.Biocoded == true).
            "AllowBiocodedWeapons",
            "AllowBiocodedApparel",
        };

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
        /// Policy name for the preset that contains all equipment (weapons, apparel, tools, shields, etc.).
        /// </summary>
        public const string EquipmentPreset = "Equipment";
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
        /// Policy name for the preset that contains all colonist corpses.
        /// </summary>
        public const string ColonistCorpsePreset = "Colonist Corpses";
        /// <summary>
        /// Policy name for the preset that contains all stranger corpses.
        /// </summary>
        public const string StrangerCorpsePreset = "Stranger Corpses";
        /// <summary>
        /// Policy name for the preset that contains all pet corpses (tame colony animals).
        /// </summary>
        public const string PetCorpsePreset = "Pet Corpses";
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
        /// Policy name for the preset that contains all things with quality below Normal (Awful and Poor).
        /// </summary>
        public const string LowQualityPreset = "Low Quality";
        /// <summary>
        /// Policy name for the preset that contains all tattered apparel and weapons (hit points at 25% or less).
        /// </summary>
        public const string TatteredPreset = "Tattered";
        /// <summary>
        /// Policy name for the preset that contains all worn-out apparel and weapons (hit points at 50% or less).
        /// </summary>
        public const string WornOutPreset = "Worn Out";
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
            CreateSimple(EquipmentPreset, "Filters all things that can be equipped (weapons, apparel, tools, shields, etc.)",
                CreateEquipmentCondition(),
                true);
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
            Toolkit.Indexing.Thing.TrackCorpseKind();
            // Lazy evaluation: corpse-kind metadata is resolved per-call from the live database, so a fresh corpse is
            // filtered immediately instead of waiting for the next snapshot cycle.
            if (!IsReplacedBySpecialThingFilterPreset("AllowCorpsesColonist"))
            {
                CreateSimple(ColonistCorpsePreset, "Filters all colonist corpses", CreateColonistCorpseCondition(), false, isLazy: true);
            }
            if (!IsReplacedBySpecialThingFilterPreset("AllowCorpsesStranger"))
            {
                CreateSimple(StrangerCorpsePreset, "Filters all stranger corpses", CreateStrangerCorpseCondition(), false, isLazy: true);
            }
            CreateSimple(PetCorpsePreset, "Filters all pet corpses (tame colony animals)", CreatePetCorpseCondition(), false, isLazy: true);
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
                    // Davai's Sorted Categories does the same job as Bad Meat Category: it moves foul meat
                    // (human, insect, twisted, toxic, etc.) out of MeatRaw into its Nasty meat category, so
                    // IsMeat becomes false for those defs. Include that category as well to keep matching them.
                    if (ToolkitConstants.Mods.DavaiSortedCategories.IsLoaded)
                    {
                        _ = foulMeat.Or
                                   .Compare.Self()
                                   .With.InThingCategory(ToolkitConstants.Mods.DavaiSortedCategories.NastyMeatCategoryDefName);
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
            if (!IsReplacedBySpecialThingFilterPreset("AllowRotten"))
            {
                CreateSimple(RottingPreset, "Filters all things that are rotting or fully decomposed (e.g. skeletons)",
                    CreateRottingCondition(),
                    false);
            }
            if (!IsReplacedBySpecialThingFilterPreset("AllowSmeltable"))
            {
                CreateSimple(SmeltablePreset, "Filters all things that can be smelted at a smelter (matches vanilla Allow Smeltable)",
                    CreateSmeltableCondition(),
                    false, isLazy: false);
            }
            CreateSimple(DeterioratesPreset, "Filters all items that deteriorate when left outside",
                CreateStatCondition(StatDefOf.DeteriorationRate, GreaterOperatorType.DefaultTypeName, 0),
                true);
            if (!IsReplacedBySpecialThingFilterPreset("AllowBiocodedWeapons"))
            {
                CreateSimple(BiocodedPreset, "Filters all biocoded items (bound to a specific pawn)",
                    BuildConditions(builder =>
                        builder.Compare.Comp($"{typeof(CompBiocodable).FullName}{CompReferenceType.PathSeparator}{nameof(CompBiocodable.Biocoded)}")
                               .With.True()
                    ),
                    false);
            }
            CreateSimple(NoQualityPreset, "Filters all items with no quality (raw resources, components, etc.)",
                BuildConditions(builder =>
                    builder.Compare.Comp(typeof(CompQuality))
                           .With.Null()
                ),
                false, isLazy: false);
            CreateSimple(LowQualityPreset, "Filters all things whose quality is below Normal (Awful and Poor)",
                CreateLowQualityCondition(),
                false, isLazy: false);
            // HitPointPercentage metadata is required by the damaged equipment presets. Track it here so the presets
            // work even when storage filtering (which also tracks it) is disabled; registering the same indexer
            // twice is a no-op.
            Toolkit.Indexing.Thing.TrackHitPointPercentage();
            CreateSimple(TatteredPreset, "Filters all apparel and weapons that are tattered (hit points at 25% or less)",
                CreateWornEquipmentCondition(20f),
                false, isLazy: false);
            CreateSimple(WornOutPreset, "Filters all apparel and weapons that are worn out (hit points at 50% or less)",
                CreateWornEquipmentCondition(50f),
                false, isLazy: false);
            CreateSimple(AboveTechLevelPreset, "Filters all things whose tech level is above the tech level of the faction that owns the map",
                CreateTechLevelCondition(GreaterOperatorType.DefaultTypeName),
                false, isLazy: false);
            CreateSimple(BelowTechLevelPreset, "Filters all things whose tech level is below the tech level of the faction that owns the map",
                CreateTechLevelCondition(LesserOperatorType.DefaultTypeName),
                false, isLazy: false);
            Presets((name, description, template, settings) =>
            {
                CreatePreset(name, description, template, settings);
            });

            // Special thing filter presets are a separate opt-in so players can keep the smaller preset list.
            if (DynamicFiltersToolkit.Settings.EnableSpecialThingFilterPresets)
            {
                CreateSpecialThingFilterPresets();
            }
        }

        /// <summary>
        /// Returns whether the special thing filter with the given defName duplicates a built-in condition preset
        /// while special thing filter presets are enabled, in which case the built-in preset is skipped and the
        /// special thing filter preset takes its place.
        /// </summary>
        /// <param name="defName">The defName of the <see cref="SpecialThingFilterDef"/>.</param>
        /// <returns><c>true</c> when the built-in preset should be skipped; otherwise, <c>false</c>.</returns>
        public static bool IsReplacedBySpecialThingFilterPreset(string defName)
        {
            if (!DynamicFiltersToolkit.Settings.EnableSpecialThingFilterPresets)
            {
                return false;
            }
            if (!IsDuplicateSpecialThingFilter(defName))
            {
                return false;
            }
            return DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(defName) != null;
        }

        /// <summary>
        /// Creates the condition that mirrors a single special thing filter: the thing itself (Self) checked
        /// against the given special thing filter def via the MatchesThingFilter operator, which delegates to the
        /// def's worker — the exact check the vanilla stockpile UI performs.
        /// </summary>
        /// <param name="defName">The defName of the <see cref="SpecialThingFilterDef"/> to check against.</param>
        /// <returns>An array containing the single <see cref="SimpleFilterPolicyCondition"/>.</returns>
        public static SimpleFilterPolicyCondition[] CreateSpecialThingFilterCondition(string defName)
        {
            defName = Guard.NotNullOrWhitespace(defName, nameof(defName));

            var conditionDef = ConditionBuilder.Build(builder =>
                builder.Compare.Self()
                       .With.MatchesThingFilter()
                       .To.SpecialThingFilter(defName));

            return new[] { SimpleFilterPolicyCondition.FromDef(conditionDef) };
        }

        /// <summary>
        /// Returns whether a special thing filter def duplicates one of the built-in condition presets. While
        /// special thing filter presets are enabled, the special thing filter preset wins and the built-in preset
        /// is skipped (see <see cref="IsReplacedBySpecialThingFilterPreset(string)"/>).
        /// </summary>
        /// <param name="defName">The defName of the <see cref="SpecialThingFilterDef"/>.</param>
        /// <returns><c>true</c> when a built-in preset duplicates this special thing filter; otherwise, <c>false</c>.</returns>
        public static bool IsDuplicateSpecialThingFilter(string defName)
        {
            return defName != null && DuplicateSpecialThingFilterDefNames.Contains(defName);
        }

        /// <summary>
        /// Creates read-only presets that mirror every loaded special thing filter (vanilla, expansions, and modded).
        /// Each preset is a thing-level SimpleFilterPolicy with a single condition — <c>Self MatchesThingFilter
        /// [SpecialThingFilterDef]</c> — that delegates to the def's worker, so each preset matches exactly what the
        /// corresponding stockpile checkbox matches. When a preset is activated, the Simple Filter Policy registers
        /// its own collection, so complex filter policies can include or exclude it like any other policy. Defs
        /// without a worker class (a config error) are skipped and logged; built-in presets that duplicate a special
        /// thing filter yield to it (see <see cref="IsReplacedBySpecialThingFilterPreset(string)"/>). Calling this
        /// method more than once per session is a no-op.
        /// </summary>
        public static void CreateSpecialThingFilterPresets()
        {
            if (_specialThingFilterPresetsActivated)
            {
                return;
            }
            _specialThingFilterPresetsActivated = true;

            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in DefDatabase<SpecialThingFilterDef>.AllDefs)
            {
                CreateSpecialThingFilterPreset(def, usedTitles);
            }
        }

        /// <summary>
        /// Registers a single special thing filter def as a read-only preset backed by the def's worker. Duplicate
        /// labels (e.g. "allow smeltable" under both Apparel and Weapons) are disambiguated with the parent
        /// category in the title.
        /// </summary>
        /// <param name="def">The special thing filter def to mirror. Can be null.</param>
        /// <param name="usedTitles">Titles already in use by other special thing filter presets, used to disambiguate duplicate labels. When null, a new set is created.</param>
        public static void CreateSpecialThingFilterPreset(SpecialThingFilterDef def, ISet<string> usedTitles = null)
        {
            if (def == null)
            {
                return;
            }
            if (def.workerClass == null)
            {
                Logging.Log($"Skipping special thing filter preset '{def.defName}': no worker class defined.");
                return;
            }

            usedTitles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var condition = CreateSpecialThingFilterCondition(def.defName);
            var title = GetSpecialThingFilterPresetTitle(def, usedTitles);
            CreateSimple(title, BuildSpecialThingFilterPresetDescription(def), condition, false, isLazy: true, presetKind: SpecialThingFilterPresetKind);
        }

        /// <summary>
        /// Builds the user-facing description from the def's own description (when present) and falls back to a
        /// generated sentence that names the filter. Adds the parent category for context, so filters whose labels
        /// only make sense inside their stockpile category (e.g. "smeltable" under Apparel vs Weapons) stay
        /// understandable as standalone presets.
        /// </summary>
        /// <param name="def">The special thing filter def to describe. Must not be null.</param>
        /// <returns>The description shown in the templates UI.</returns>
        private static string BuildSpecialThingFilterPresetDescription(SpecialThingFilterDef def)
        {
            var label = string.IsNullOrWhiteSpace(def.label) ? def.defName : def.label;
            var description = string.IsNullOrWhiteSpace(def.description)
                ? $"Mirrors the \"{label}\" special thing filter."
                : def.description;

            var category = def.parentCategory;
            if (category != null && !string.Equals(category.defName, "Root", StringComparison.Ordinal))
            {
                var categoryLabel = string.IsNullOrWhiteSpace(category.label) ? category.defName : category.label;
                description += $" Applies to the {categoryLabel} category.";
            }
            return description;
        }

        /// <summary>
        /// Derives the preset title from the def's label (title-cased), falling back to the defName when the label
        /// is missing. When the label collides with an already-registered special thing filter preset, the parent
        /// category is appended (e.g. "Allow Smeltable (Apparel)"), and the defName is used when that still
        /// collides.
        /// </summary>
        /// <param name="def">The special thing filter def to title. Must not be null.</param>
        /// <param name="usedTitles">Titles already in use, updated with the derived title. Must not be null.</param>
        /// <returns>The preset title without the "[Preset]" prefix.</returns>
        private static string GetSpecialThingFilterPresetTitle(SpecialThingFilterDef def, ISet<string> usedTitles)
        {
            var baseTitle = string.IsNullOrWhiteSpace(def.label)
                ? def.defName
                : ToTitleCase(def.label);

            var title = baseTitle;
            if (usedTitles.Contains(title))
            {
                var category = def.parentCategory;
                var categoryLabel = string.IsNullOrWhiteSpace(category?.label) ? category?.defName : category.label;
                title = string.IsNullOrWhiteSpace(categoryLabel)
                    ? $"{baseTitle} ({def.defName})"
                    : $"{baseTitle} ({categoryLabel.CapitalizeFirst()})";

                if (usedTitles.Contains(title))
                {
                    title = $"{baseTitle} ({def.defName})";
                }
            }

            usedTitles.Add(title);
            return title;
        }

        /// <summary>
        /// Capitalizes the first letter of every whitespace-separated word, e.g. "allow colonist corpses" becomes
        /// "Allow Colonist Corpses". Unlike <see cref="GenText.CapitalizeFirst(string)"/>, which only capitalizes
        /// the very first character of the string.
        /// </summary>
        /// <param name="text">The text to title-case. Can be null or empty.</param>
        /// <returns>The title-cased text.</returns>
        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                words[i] = words[i].CapitalizeFirst();
            }
            return string.Join(" ", words);
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
        /// Creates thing-level conditions for things that are currently rotting or fully decomposed.
        /// A thing matches when its <see cref="CompRottable"/> stage is <see cref="RotStage.Rotting"/> or
        /// <see cref="RotStage.Dessicated"/> (a fully decomposed corpse, e.g. a skeleton). Fresh things do not match.
        /// Collections evaluate <see cref="IIndexed{T}"/> entries, so the Comp reference resolves the comp from the
        /// indexed value's thing.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateRottingCondition()
        {
            return BuildConditions(builder =>
                builder.Compare.Comp($"{typeof(CompRottable).FullName}{CompReferenceType.PathSeparator}{nameof(CompRottable.Stage)}")
                       .With.In()
                       .To.Value(new[] { RotStage.Rotting, RotStage.Dessicated })
            );
        }

        /// <summary>
        /// Creates a thing-level condition that matches corpses of ghouls (Anomaly). A thing is a ghoul corpse when
        /// its indexed <see cref="ToolkitConstants.Thing.IsGhoulCorpse"/> metadata is true, which is only set for
        /// <see cref="Corpse"/>s whose <see cref="Corpse.InnerPawn"/> carries the Anomaly "Ghoul" hediff. Ghouls are
        /// transformed humans, so their corpses share the Human corpse def and cannot be identified by def alone. The
        /// condition reads the metadata set by <see cref="Toolkit.Indexing.Thing.TrackIsGhoulCorpse"/>, so the preset
        /// must run with lazy evaluation, resolving the metadata per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateGhoulCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsGhoulCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
        }

        /// <summary>
        /// Creates a thing-level condition that matches colonist corpses, mirroring the vanilla "Allow Colonist corpses"
        /// special thing filter. A thing is a colonist corpse when its indexed <see cref="ToolkitConstants.Thing.IsColonistCorpse"/>
        /// metadata is true, which is only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike corpses
        /// whose inner pawn was a free colonist. The condition reads metadata, so the preset must run with lazy
        /// evaluation, resolving the metadata per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateColonistCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsColonistCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
        }

        /// <summary>
        /// Creates a thing-level condition that matches stranger corpses, mirroring the vanilla "Allow Stranger corpses"
        /// special thing filter. A thing is a stranger corpse when its indexed <see cref="ToolkitConstants.Thing.IsStrangerCorpse"/>
        /// metadata is true, which is only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike corpses
        /// whose inner pawn did not belong to the player faction. The condition reads metadata, so the preset must run with
        /// lazy evaluation, resolving the metadata per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateStrangerCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsStrangerCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
        }

        /// <summary>
        /// Creates a thing-level condition that matches pet corpses (tame colony animals). A thing is a pet corpse when its
        /// indexed <see cref="ToolkitConstants.Thing.IsPetCorpse"/> metadata is true, which is only set by
        /// <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for corpses whose inner pawn was a tame animal of the player
        /// faction (the vanilla <see cref="Pawn.IsColonyAnimal"/> concept). The condition reads metadata, so the preset runs with
        /// lazy evaluation, resolving it per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreatePetCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsPetCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
        }

        /// <summary>
        /// Creates a thing-level condition that matches slave corpses, mirroring the vanilla "Allow Slave corpses" special
        /// thing filter. A thing is a slave corpse when its indexed <see cref="ToolkitConstants.Thing.IsSlaveCorpse"/> metadata
        /// is true, which is only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike corpses whose inner
        /// pawn was a player-faction slave (Ideology). The condition reads metadata, so the preset must run with lazy
        /// evaluation, resolving the metadata per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateSlaveCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsSlaveCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
        }

        /// <summary>
        /// Creates a thing-level condition that matches unnatural corpses (Anomaly), mirroring the vanilla "Allow Unnatural
        /// corpses" special thing filter. A thing is an unnatural corpse when its indexed
        /// <see cref="ToolkitConstants.Thing.IsUnnaturalCorpse"/> metadata is true, which is only set by
        /// <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for <see cref="UnnaturalCorpse"/>s. The condition reads metadata,
        /// so the preset must run with lazy evaluation, resolving the metadata per-call from the live database.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateUnnaturalCorpseCondition()
        {
            return CreatePropertyCondition(ToolkitConstants.Thing.IsUnnaturalCorpse.Name, EqualsOperatorType.DefaultTypeName, true);
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
        /// Creates def-level conditions that match anything a pawn can equip: weapons (melee and ranged, including
        /// tool-like items such as thrumbo horns and elephant tusks), primary-slot equipment such as shields (marked
        /// via <see cref="ThingDef.equipmentType"/>), and apparel. All three operands are def members, so the preset
        /// must run with def-level (ThingDef) evaluation. Collections evaluate <see cref="IIndexed{T}"/> entries, so
        /// each path segment resolves from the indexed value's def member.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateEquipmentCondition()
        {
            return BuildConditions(builder =>
                builder.Compare.Indexed(nameof(ThingDef.IsWeapon))
                       .With.True()
                       .Or
                       .Compare.Indexed(nameof(ThingDef.equipmentType))
                       .With.Equal()
                       .To.Value(EquipmentType.Primary)
                       .Or
                       .Compare.Indexed(nameof(ThingDef.IsApparel))
                       .With.True()
            );
        }

        /// <summary>
        /// Creates thing-level conditions for things whose quality is below <see cref="QualityCategory.Normal"/>, i.e.
        /// Awful and Poor. A guard ensures the thing has a <see cref="CompQuality"/> first, so raw resources and other
        /// things without quality do not match. Collections evaluate <see cref="IIndexed{T}"/> entries, so the Comp
        /// references resolve the comp from the indexed value's thing.
        /// </summary>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateLowQualityCondition()
        {
            return BuildConditions(builder =>
                builder.Compare.Comp(typeof(CompQuality))
                       .With.NotNull()
                       .And
                       .Compare.Comp($"{typeof(CompQuality).FullName}{CompReferenceType.PathSeparator}{nameof(CompQuality.Quality)}")
                       .With.LessThan()
                       .To.Value(QualityCategory.Normal)
            );
        }

        /// <summary>
        /// Creates thing-level conditions for apparel and weapons that are damaged, i.e. apparel and weapons whose
        /// current hit points are at or below the given percentage of their maximum. Only defs that are apparel or
        /// weapons can match, and only things that use hit points (which carry the indexed
        /// <see cref="ToolkitConstants.Thing.HitPointPercentage"/> metadata) are considered, so raw resources and
        /// other non-equipment items never match. Collections evaluate <see cref="IIndexed{T}"/> entries, so the
        /// first path segments resolve from the indexed value's members (def) and the hit point percentage resolves
        /// from the indexed metadata. The condition reads metadata, so the preset must run with eager (non-lazy)
        /// evaluation where the indexed items carry that metadata.
        /// </summary>
        /// <param name="maxHitPointPercentage">The maximum hit point percentage (0-100) that still counts as
        /// damaged. A thing matches when its hit points are at or below this percentage.</param>
        /// <returns>An array of SimpleFilterPolicyCondition objects.</returns>
        public static SimpleFilterPolicyCondition[] CreateWornEquipmentCondition(float maxHitPointPercentage)
        {
            return BuildConditions(builder =>
                builder.Group(equipment => equipment
                           .Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.IsApparel)}")
                           .With.True()
                           .Or
                           .Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.IsWeapon)}")
                           .With.True())
                       .And
                       .Compare.Indexed(ToolkitConstants.Thing.HitPointPercentage.Name)
                       .With.LessThanOrEqual()
                       .To.Value(maxHitPointPercentage)
            );
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
        /// <param name="conditions">The conditions to apply to the policy.</param>
        /// <param name="thingDef"><inheritdoc cref="BaseCollectionFilterPolicySettings.ThingDef"/></param>
        /// <param name="requireMapContext"><inheritdoc cref="BaseCollectionFilterPolicySettings.RequireMapContext"/></param>
        /// <param name="isLazy"><inheritdoc cref="BaseCollectionFilterPolicySettings.IsLazy"/></param>
        /// <param name="presetKind"><inheritdoc cref="DelegatedPolicyPreset.Kind"/></param>
        public static void CreateSimple(string presetName, string description, SimpleFilterPolicyCondition[] conditions, bool thingDef = true, bool requireMapContext = false, bool isLazy = true, string presetKind = "Preset")
        {
            var settings = new SimpleFilterPolicySettings()
            {
                Conditions = conditions.ToList(),
                ThingDef = thingDef,
                RequireMapContext = requireMapContext,
                LazyEvaluation = isLazy
            };
            var template = SimpleFilterPolicy.Instance;
            CreatePreset<SimpleFilterPolicy>(presetName, description, template, settings, presetKind);
        }

        /// <summary>
        /// Activates a preset with the given name and provider.
        /// </summary>
        /// <param name="presetName">The name of the preset to activate.</param>
        /// <param name="description">The description of the preset.</param>
        /// <param name="policy">The policy template to use for the preset.</param>
        /// <param name="settings">The settings for the policy</param>
        /// <param name="presetKind"><inheritdoc cref="DelegatedPolicyPreset.Kind"/></param>
        public static void CreatePreset<T>(string presetName, string description, T policy, IExposable settings, string presetKind = "Preset") where T : IDynamicPolicyTemplate
        {
            var preset = new DelegatedPolicyPreset<T>(presetName, description, policy, settings);
            preset.Kind = presetKind;
            DynamicFiltersToolkit.Templates.AddTemplate(preset);
        }
    }
}
