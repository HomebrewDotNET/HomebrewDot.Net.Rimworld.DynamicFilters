---
type: concept
title: Presets Overview
description: DynamicFilterPresets, the static catalog of built-in read-only policies covering resources, food, corpses, medical, drinks, perishability, tech level and more, plus PresetPatches that add mod-gated presets for Odyssey and Alpha Bees.
tags: [presets]
---

# Presets Overview — `DynamicFilterPresets`

`DynamicFilterPresets` (`DynamicFilterPresets.cs`) is a static class containing the built-in presets: named, ready-made policies registered as read-only templates. It is enabled by the `EnablePresets` setting; `DynamicFilterPresets.ActivatePresets()` runs on enable and on game load (see [Mod Entry Point](../mod/entrypoint.md)).

## Mechanism

- `Presets` is a `Action<Action<string, string, IDynamicPolicyTemplate, IExposable>>` multicast delegate, defaulting to a no-op. `AddPresetProvider(action)` appends another provider (used by [PresetPatches](#presetpatches) and other mods).
- `ActivatePresets()` first creates the built-in presets via `CreateSimple(...)` (each registers a `DelegatedPolicyPreset<SimpleFilterPolicy>` template through `CreatePreset<T>`), then invokes every provider delegate with an activator that calls `CreatePreset` — so mod-added presets are registered in the same pass.
- `CreateSimple(name, description, conditions, thingDef)` wraps conditions in `SimpleFilterPolicySettings` and registers a `DelegatedPolicyPreset` template (see [Configuration templates](../configuration/templates.md)).
- `CreatePreset<T>(presetName, description, T policy, IExposable settings)` builds `new DelegatedPolicyPreset<T>(presetName, description, policy, settings)` and adds it to `DynamicFiltersToolkit.Templates`.
- `CreateSpecialThingFilterPresets()` runs last in `ActivatePresets()` and registers one `SpecialThingFilterPreset` template per loaded `SpecialThingFilterDef` (see [Special Thing Filter Presets](#special-thing-filter-presets)).

Presets appear in the Templates tab as `[Preset] {name}` templates and, when activated, are read-only (no edit/rename in the Policies tab).

## Catalog of built-in presets

| Constant | Policy name | Rule (conditions) | Applies to |
|---|---|---|---|
| `ResourcePreset` | Resources | `CountAsResource == true` | Def |
| `MeatPreset` | Meats | `IsMeat == true` | Def |
| `MetalPreset` | Metallic | `IsMetal == true` | Def |
| `WoodyPreset` | Wood | `stuffCategories contains Woody` | Def |
| `StonyPreset` | Stony | `stuffCategories contains Stony` | Def |
| `FabricPreset` | Fabric | `stuffCategories contains Fabric` | Def |
| `LeatheryPreset` | Leather | `stuffCategories contains Leathery` | Def |
| `PlantMatterPreset` | Plant Matter | in ThingCategory `PlantMatter` | Def |
| `IngestiblePreset` | Ingestible | `IsIngestible == true` | Def |
| `FoodPreset` | Food | `IsNutritionGivingIngestible == true` | Def |
| `MealPreset` | Meals | `ingestible.preferability in {MealTerrible..MealLavish}` | Def |
| `GoodMealPreset` | Good Meals | preferability in {MealSimple, MealFine, MealLavish} | Def |
| `SnackPreset` | Snacks | `preferability == RawTasty` OR `ingestible.joy > 0` | Def |
| `IsMedicinalPreset` | Medicinal | `IsMedicine == true` | Def |
| `IsApparelPreset` | Apparel | `IsApparel == true` | Def |
| `IsWeaponPreset` | Weapons | `IsWeapon == true` | Def |
| `IsMeleeWeaponPreset` | Melee Weapons | `IsMeleeWeapon == true` | Def |
| `IsRangedWeaponPreset` | Ranged Weapons | `IsRangedWeapon == true` | Def |
| `ConstructionPreset` | Construction Materials | indexed `IsConstructionMaterial == true` (tracked metadata, updated on research) | Def |
| `ExplosivesPreset` | Explosives | comp `CompProperties_Explosive`: `explodeOnDestroyed == true` OR `startWickOnDamageTaken.Count > 0` OR `startWickHitPointsPercent > 0` | Def |
| `FlammablePreset` | Flammable | stat `Flammability > 0` | Def |
| `ButcheryCorpsePreset` | Butchery Corpses | `IsCorpse && !race.Humanlike && !race.IsMechanoid`, excluding robot/drone corpse categories when Big and Small / VQE Drone Factory / Odyssey are loaded | Def |
| `HumanoidCorpsePreset` | Humanoid Corpses | `IsCorpse && race.Humanlike` | Def |
| `MechanoidCorpsePreset` | Mechanoid Corpses | `IsCorpse && race.IsMechanoid` | Def |
| `FoulMeatPreset` | Foul Meat | `IsFoul && IsMeat` (OR in `MeatBad` category when Bad Meat Category mod loaded) | Def |
| `FoulLeatherPreset` | Foul Leather | `IsFoul && IsLeather` (OR in `LeatherBad` when Bad Leather Category mod loaded) | Def |
| `IsMedicalPreset` | Medical Items | tracked `IsMedical == true` | Def |
| `IsSurgicalPreset` | Surgical Parts | tracked `IsSurgical == true` | Def |
| `DrinksPreset` | Drinks | tracked `IsDrink == true` | Def |
| `AlcoholicDrinksPreset` | Alcoholic Drinks | tracked `IsAlcoholic == true` | Def |
| `NonAlcoholicDrinksPreset` | Non-Alcoholic Drinks | `IsDrink && !IsAlcoholic` | Def |
| `CoffeeAndTeaPreset` | Coffee & Tea | `IsDrink && defName matches (?i)(coffee|tea)` | Def |
| `PerishesPreset` | Perishes | has `CompProperties_Rottable` | Def |
| `RottingPreset` | Rotting | thing comp `CompRottable.Stage in {Rotting, Dessicated}` | Thing |
| `SmeltablePreset` | Smeltable | `def.smeltable && (!def.MadeFromStuff || Stuff.smeltable)` — mirrors `Thing.Smeltable` / vanilla "Allow Smeltable" | Thing |
| `DeterioratesPreset` | Deteriorates | stat `DeteriorationRate > 0` | Def |
| `BiocodedPreset` | Biocoded | comp `CompBiocodable.Biocoded == true` | Thing |
| `NoQualityPreset` | No Quality | no `CompQuality` on def | Thing |
| `AboveTechLevelPreset` | Above TechLevel | thing def techLevel above map owner faction's techLevel (see [Preset Conditions](conditions.md)) | Thing |
| `BelowTechLevelPreset` | Below TechLevel | thing def techLevel below map owner faction's techLevel | Thing |

Thing-level presets (`thingDef: false`) match per-instance state (rot, smeltable-by-stuff, biocode, quality, tech level); Def-level presets match `ThingDef` properties. Because [CollectionPolicy](../policies/collection-policy.md) def filters are allow-list driven while thing filters are evaluated per-call, thing presets are enforced lazily in `ThingFilter.Allows` (see [Storage Policy Map Patcher](../storage/storage-policy-map-patcher.md)).

## Special Thing Filter Presets

When `Settings.EnableSpecialThingFilterPresets` is set (offered in the settings UI only while presets are enabled), `ActivatePresets()` calls `CreateSpecialThingFilterPresets()`, which iterates `DefDatabase<SpecialThingFilterDef>.AllDefs` and registers one read-only preset per loaded def — the stockpile "Allow ..." checkboxes such as allow fresh, allow colonist corpses, allow smeltable, allow clean/tainted apparel, the Ideology food filters, the Biotech mechanoid corpse filters, the Anomaly filters, the book filters, and everything mods add (e.g. Big and Small's robot corpse filters, Mechanoid Upgrades' tier filters).

- Each preset is a thing-level `SimpleFilterPolicy` with a single condition, `Self MatchesThingFilter [SpecialThingFilterDef]` (see [Special Thing Filter Presets](../policies/special-thing-filter-preset.md)). The `MatchesThingFilter` operator (owned by the Toolkit and registered globally by `Toolkit.ConfigureServices()`) delegates to `def.Worker.Matches(thing)` — the exact worker the vanilla `ThingFilter.Allows` check uses — so semantics are 1:1 with the game's own filter (including per-instance state such as rot stage or deadman's apparel) and modded defs with custom workers work unchanged. The operator is compileable, so the collection comparator compiles the check into a delegate instead of invoking it reflectively.
- Each def registers a `[ThingFilter]` preset (preset kind `ThingFilter`); activating it makes the Simple Filter Policy register its own collection, so Complex Filter Policies can include/exclude it by policy name.
- Defs without a `workerClass` (a config error) are skipped and logged. Where a special thing filter duplicates a built-in condition preset, the special thing filter preset wins: the built-in preset is skipped via `IsReplacedBySpecialThingFilterPreset(defName)` (requires the STF preset setting plus the def being loaded) — `AllowRotten` yields to Rotting's replacement, `AllowCorpsesColonist`/`AllowCorpsesStranger`/`AllowCorpsesSlave`/`AllowCorpsesUnnatural` to the corpse presets, `AllowSmeltable`/`AllowSmeltableApparel` to Smeltable, and `AllowBiocodedWeapons`/`AllowBiocodedApparel` to Biocoded.
- Preset titles are derived from `def.label` (title-cased); duplicate labels (e.g. "allow smeltable" under both Apparel and Weapons) are disambiguated with the parent category, falling back to the defName. Descriptions prefer `def.description` and note the parent category.
- `CreateSpecialThingFilterPresets()` is a once-per-session operation, so toggling settings within a session never registers duplicates.

## Metadata trackers required by presets

`ActivatePresets` also enables Toolkit metadata trackers used by presets before they are used:

- `Toolkit.Indexing.Def.Thing.TrackIsConstructionMaterial()` (Construction)
- `Toolkit.Indexing.Def.Thing.TrackIsFoul()` (Foul Meat/Leather)
- `Toolkit.Indexing.Def.Thing.TrackIsDrink()` and `TrackIsAlcoholic()` (Drinks)
- `Toolkit.Indexing.Def.Thing.TrackIsMedical()` and `TrackIsSurgical()` (Medical/Surgical)

These are the same mechanism as `EnableStorageFiltering` uses for `TrackHitPointPercentage`/`TrackModId`/`TrackMap` (see [Mod Entry Point](../mod/entrypoint.md)).

## PresetPatches

`Patches/PresetPatches.cs` is a `[StaticConstructorOnStartup]` class that registers a `OnGameLoadedTrigger` hook (priority `byte.MinValue`) adding mod-gated presets through `DynamicFilterPresets.AddPresetProvider`:

- **Odyssey** (`ToolkitConstants.Odyssey.IsLoaded`): calls the tracker `Toolkit.Indexing.Thing.TrackIsUnique()` and adds `UniquePreset` ("Uniques") — created via `CreateSimple(UniquePreset, ..., CreatePropertyCondition(ToolkitConstants.Thing.IsUnique.Name, TrueOperatorType.DefaultTypeName, null), thingDef: false)`, i.e. a thing-level `IsUnique == true` condition (includes modded uniques).
- **Alpha Bees** (`ToolkitConstants.Mods.Alpha.Bees.IsLoaded`): adds `QueenBeePreset` ("Bee Queens") and `DroneBeePreset` ("Bee Drones") — both built with `CreateSimple(..., thingDef: true)` (def-level) from `ConditionBuilder` clauses matching `defName` against the private regex constant `BeeDefPrefixRegex` (`(?i)^RB_`) AND `label` against `BeeQueenRegex` (`(?i)Queen$`) or `BeeDroneRegex` (`(?i)Drone$`).

The three preset-name constants are `UniquePreset = "Uniques"`, `QueenBeePreset = "Bee Queens"`, `DroneBeePreset = "Bee Drones"`.

Preset providers are invoked on the next `ActivatePresets()` (enable or game load).

## Related pages

- [Preset Conditions](conditions.md) — the condition factories used above.
- [Policies](../policies/simple-filter-policy.md) — the templates presets delegate to.
- [Unit Tests](../testing/unit-tests.md) / [Integration Tests](../testing/integration-tests.md) — preset behavior coverage.
