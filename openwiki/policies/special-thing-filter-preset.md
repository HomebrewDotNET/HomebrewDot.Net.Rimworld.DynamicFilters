---
type: concept
title: Special Thing Filter Presets
description: The worker-backed preset catalog that mirrors every loaded SpecialThingFilterDef through the MatchesThingFilter operator, plus the duplicate filtering that lets the special thing filters win over built-in presets.
tags: [presets, policies]
---

# Special Thing Filter Presets

`DynamicFilterPresets.CreateSpecialThingFilterPresets()` registers one read-only `[ThingFilter]` preset per loaded `SpecialThingFilterDef` — the stockpile "Allow ..." checkboxes like "allow fresh", "allow colonist corpses", "allow smeltable", "allow clean apparel", and everything mods add. The catalog always matches what is loaded: Core, expansions, and enabled mods.

## Condition, not reimplementation

Each preset is a thing-level `SimpleFilterPolicy` with a single condition built by `CreateSpecialThingFilterCondition(defName)`:

```csharp
builder.Compare.Self()
       .With.MatchesThingFilter()
       .To.SpecialThingFilter("AllowFresh");
```

The condition is `Self MatchesThingFilter [SpecialThingFilterDef]AllowFresh`, evaluated lazily against the live thing:

- **`MatchesThingFilter`** — `IOperatorType` owned by the Toolkit (`Toolkit/Comparing/Components/MatchesThingFilterOperatorType.cs`, `DefaultTypeName = "MatchesThingFilter"`). It resolves the left operand to the thing and the right operand to the `SpecialThingFilterDef`, then calls `def.Worker.Matches(thing)` — the very same worker vanilla runs in `ThingFilter.Allows`. Semantics are 1:1, including per-instance state (rot stage, `Apparel.WornByCorpse`, `CompBiocodable` flags, pawn gender, corpse factions), and modded defs with custom workers work unchanged. A null thing, a null def, or a def without a `workerClass` never matches. The operator implements `IOperatorTypeCompileable`, so the collection comparator compiles the check into a delegate (safe-casting the left operand and guarding the def/worker class) instead of resolving members reflectively per call.
- **`[SpecialThingFilterDef]` reference** — `DefReferenceType<SpecialThingFilterDef>` (shipped in the Toolkit; the `SpecialThingFilter(defName)` helper lives on `DefReferenceTypeExtensions` next to `ThingCategory`/`StuffCategory`, and the `MatchesThingFilter()` helper lives in the operator type's file).

Both services are registered globally by `Toolkit.ConfigureServices()`, so the operator, the def reference, and the condition vocabulary are available to every mod on the Toolkit — not just Dynamic Filters.

## Collections for include/exclude

No extra collection is registered: each preset is a `SimpleFilterPolicy`, and when the preset is activated the Simple Filter Policy registers its own named collection under the policy name (see [Simple Filter Policy](simple-filter-policy.md)). Complex Filter Policies can therefore **include or exclude** an activated special thing filter preset by its policy name, exactly like any other policy.

## Registration, titles, duplicates

- `CreateSpecialThingFilterPreset(def, usedTitles)` skips null defs and defs without a `workerClass` (config error) with a log line, builds the condition, and registers the preset through `CreateSimple(title, description, condition, thingDef: false, isLazy: true, presetKind: "ThingFilter")` — the kind is what gives the preset its `[ThingFilter]` UI prefix and storage-key segment.
- Titles derive from `def.label`, title-cased ("allow colonist corpses" → "Allow Colonist Corpses"). Duplicate labels — e.g. "allow smeltable" under both Apparel and Weapons — get the parent category appended ("Allow Smeltable (Weapons)"), falling back to the defName on a further collision.
- Descriptions prefer `def.description` and fall back to a generated sentence; the parent category is appended unless it is `Root`.
- **Duplicate filtering** — `IsDuplicateSpecialThingFilter(defName)` reports which special thing filters duplicate a built-in preset, and `IsReplacedBySpecialThingFilterPreset(defName)` decides (STF preset setting enabled + def loaded) whether the built-in preset yields: `AllowRotten` (Rotting), `AllowCorpsesColonist`/`AllowCorpsesStranger`/`AllowCorpsesSlave`/`AllowCorpsesUnnatural` (corpse presets), `AllowSmeltable`/`AllowSmeltableApparel` (Smeltable), and `AllowBiocodedWeapons`/`AllowBiocodedApparel` (Biocoded). The special thing filter preset always wins.
- `CreateSpecialThingFilterPresets()` is a once-per-session operation (guarded by `_specialThingFilterPresetsActivated`), so toggling settings within a session never registers duplicates.

## Gating

The presets are created inside `DynamicFilterPresets.ActivatePresets()` only when `DynamicFiltersToolkit.Settings.EnableSpecialThingFilterPresets` is set — and `ActivatePresets()` itself only runs while `EnablePresets` is on. The settings UI only offers the toggle while presets are enabled, so the special thing filter presets are never visible without presets.

## Related pages

- [Presets Overview](../presets/overview.md) — where the presets are registered and how they fit the preset catalog.
- [Simple Filter Policy](simple-filter-policy.md) — the template each preset delegates to, and whose activation registers the collection.
- [Complex Filter Policy](complex-filter-policy.md) — the include/exclude consumer of activated policy collections.
