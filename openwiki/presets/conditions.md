---
type: concept
title: Preset Conditions
description: The public condition factory methods on DynamicFilterPresets, how property/stat/comp/tech-level/rotting/smeltable conditions are built with ConditionBuilder, and the index-reference rules they rely on.
tags: [presets]
---

# Preset Conditions

`DynamicFilterPresets` exposes public factory methods that build `SimpleFilterPolicyCondition[]` arrays. They are the extension surface for other mods adding presets and are exercised directly by the unit and integration tests.

## Reference types and index rules

Conditions are built with the Toolkit's `ConditionBuilder` and reference types (`IndexedReferenceType`, `PropertyReferenceType`, `ValueReferenceType`, `StatReferenceType`, `CompReferenceType`, `SelfReferenceType`). Key rule for collections: **collections evaluate `IIndexed<T>` entries**, so the first path segment resolves from the indexed value's member or metadata and the remainder is traversed on the resolved object. That is why tech-level and smeltable conditions use `Indexed` paths such as `def.techLevel` or `Thing.def.smeltable`.

## Factories

### `CreatePropertyCondition(string propertyName, string @operator, object value)`

`Compare.Indexed(propertyName).With.Operator(@operator).To.Value(value)` — a single condition against an indexed property (e.g. `IsMeat`, `CountAsResource`).

### `CreateStatCondition(StatDef stat, string @operator, object value)`

`Compare.Stat(stat).With.Operator(@operator).To.Value(value)` — comparison against a thing stat (e.g. `StatDefOf.Flammability > 0`).

### `CreateCompCondition(Type compType, string properties, string @operator, object value)`

Comp reference condition: with a `properties` suffix it builds `"{compType.FullName}{CompReferenceType.PathSeparator}{properties}"` (e.g. `CompBiocodable/Biocoded`); without one it references the comp itself (null/not-null tests).

### `CreateModFilterCondition(Regex)` / `CreateModFilterCondition(string modId)`

Indexed `ToolkitConstants.Thing.ModId` matched by regex or equality. Both force `IsOr = false` on the resulting condition (making it AND with the preceding rule — used by mod integrations to combine with other conditions).

### `CreateTechLevelCondition(string @operator)` — 3-part structure

```csharp
def.techLevel != TechLevel.Undefined
AND Map.ParentFaction != null
AND def.techLevel <operator> Map.ParentFaction.def.techLevel
```

- Excludes `TechLevel.Undefined` items (guard), and things on maps without a parent faction (guard, `NotNull`).
- Both operands are `Indexed` because the evaluated entry is an `IIndexed<Thing>`: `Thing.def.techLevel` and `Thing.Map.ParentFaction.def.techLevel` (paths traversed on resolved objects).
- `GreaterOperatorType` → "Above TechLevel" preset; `LesserOperatorType` → "Below TechLevel".
- Unit tests pin the 3-condition shape, both guards, the operator, and the `To` reference (`DynamicFilterPresetsTechLevelTests`).

### `CreateRottingCondition()`

```csharp
Comp<CompRottable>.Stage in {RotStage.Rotting, RotStage.Dessicated}
```

- Comp reference path `"{typeof(CompRottable).FullName}{CompReferenceType.PathSeparator}Stage"`.
- Includes `Dessicated` so fully decomposed corpses (skeletons) match; `Fresh` does not.
- Unit tests verify structure (Comp reference, `In` operator, both stages) and evaluation behavior through the real comparator pipeline with `rotProgressInt` staged via reflection (`DynamicFilterPresetsRottingTests`).

### `CreateSmeltableCondition()`

```csharp
def.smeltable == true
AND ( def.MadeFromStuff == false OR Stuff.smeltable == true )
```

- Mirrors `Thing.Smeltable` / vanilla "Allow Smeltable": a def made from stuff is smeltable only when the stuff itself is smeltable (steel club yes, wooden club no).
- Uses an OR-group (`builder.Group(...)`) so the two inner conditions become a nested `ConditionDef` — this group is what `SimpleFilterPolicy.ValidateSettings` exempts from leaf validation.
- Returns exactly 2 conditions (outer AND + group). Unit tests pin structure and behavior for non-stuff defs, smeltable/non-smeltable stuff, and non-smeltable defs (`DynamicFilterPresetsSmeltableTests`).

### `CreateExplosiveCondition()` (private)

```csharp
Comp<CompProperties_Explosive>.explodeOnDestroyed == true
OR Comp<...>.startWickOnDamageTaken.Count > 0
OR Comp<...>.startWickHitPointsPercent > 0L
```

Matches things that can explode on death, when taking damage, or when lit on fire. The `startWickOnDamageTaken` operand traverses a `List<DamageDef>` through `.Count`; the hit-points variant compares against `0L`.

### `CreateSnackConditions()` (private)

```csharp
ingestible.preferability == FoodPreferability.RawTasty
OR ingestible.joy > 0
```

"Tasty raw or gives joy when ingested."

### `BuildConditions(Action<IConditionBuilder>)` (private)

Runs `ConditionBuilder.Build` and flattens: returns `def.Conditions.Select(FromDef)` when the def produced multiple conditions, otherwise a single-element array from the def itself — the shared normalization used by all multi-condition factories.

## Test coverage

- Structure + behavior: `DynamicFilterPresetsRottingTests`, `DynamicFilterPresetsSmeltableTests` (unit).
- Structure: `DynamicFilterPresetsTechLevelTests` (unit).
- End-to-end through collectors: the integration suite activates real presets and asserts collected def names (Meat, Metal, Ingestible, Food, Meal, GoodMeal, Snack, Medicinal, Apparel, Weapon, Melee/Ranged, Flammable, Construction, Explosive) — see [Integration Tests](../testing/integration-tests.md).

## Related pages

- [Presets Overview](overview.md) — the catalog these factories feed.
- [Simple Filter Policy](../policies/simple-filter-policy.md) — the condition model (`SimpleFilterPolicyCondition`).
