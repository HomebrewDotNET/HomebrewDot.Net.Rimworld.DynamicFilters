---
type: concept
title: Complex Filter Policy
description: ComplexFilterPolicy, the template that combines property conditions with inclusion and exclusion of other registered collections into a single CollectionDefConfig, with validation and provider activation.
tags: [policies]
---

# Complex Filter Policy — `ComplexFilterPolicy`

`ComplexFilterPolicy` (`Policies/ComplexFilterPolicy.cs`) is the richer sibling of [SimpleFilterPolicy](simple-filter-policy.md): in addition to property conditions it can include and exclude other already-registered Toolkit collections (e.g. "start from my sniper collection, exclude everything in the melee collection"). It was extracted alongside `CollectionPolicy` in commit `f1f6397`.

## Template surface

- Singleton instance `ComplexFilterPolicy.Instance`; `StorageKey` = `"{DynamicFiltersToolkit.ModId}.ComplexFilterPolicy"`; `Singleton` = `false`.
- `GetTitle()` = `"Complex Filter Policy"`; `GetShortDescription()` = "Filter for matching thing(defs) based on specified conditions on their properties and/or other collections".
- `GetLongDescription(settings)` prints errors if validation fails, otherwise `typedSettings.Collection.ToString(stringBuilder)` (the rendered `CollectionDef`).

## Settings model

`ComplexFilterPolicySettings : IExposable`:

- `bool ThingDef = true` — as in the simple policy.
- `Config` (`CollectionDefConfig`, deep-scrubbed; defaults to a new one) — the editable representation of the whole rule.
- `Collection` (property) — the runtime `CollectionDef`: either a cached static def (when created via `FromStatic(CollectionDef)`) or `Config.ToDef()`.

`CollectionDefConfig` comes from the Toolkit's collecting model and holds:

- `Conditions` — `List<ConditionDefConfig>` (property/stat/comp comparisons),
- `Inclusions` — `List<CollectionConditionDefConfig>` (collections whose contents are added),
- `Exclusions` — `List<CollectionConditionDefConfig>` (collections whose contents are removed).

## Validation

`ValidateSettings` requires at least one condition, inclusion, or exclusion ("At least one condition, inclusion, or exclusion must be defined."). Conditions are validated like the simple policy (path regex, registered operator). Collection references must have a non-empty `Name` and must resolve in `Toolkit.Collecting.GetAllCollectors()` ("Unknown collection: {name}"). Wrong settings types yield an error.

Unit tests (`ComplexFilterPolicyTests`) pin the singleton, non-singleton (`Singleton_IsFalse`), storage key, null/wrong-type/empty-config validation, empty-operator, invalid-path, unknown-operator, empty-collection-name, and description-fallback behaviors (see [Unit Tests](../testing/unit-tests.md)).

## UI

`DrawSettings` renders only the "ForThingDef" checkbox and an "Edit Collection Config" button that opens `CollectionDefConfigEditorWindow` (from the Toolkit UI) and assigns the result back to `typedSettings.Config`.

## Provider activation

`Provider.Activate` mirrors `SimpleFilterPolicy`:

1. Ensures the ThingDef or Thing indexing surface.
2. `Toolkit.Collecting.Build(name, x => { x.FromDef(_settings.Collection); return ...CollectFromSnapshot(...); })` — the collection is built from the `CollectionDef` (conditions plus inclusion/exclusion of named collections) over the snapshot table.
3. Label "Complex Filter", description from `GetLongDescription`, then `AvailableFor<Map, ThingDef>` or `<Map, Thing>` with `new CollectionPolicy(name)`.

`Deactivate` is a no-op; cleanup happens through `ActivatedPolicies.Dispose` and `CollectionPolicy.Dispose` as with the simple policy.

## Related pages

- [Simple Filter Policy](simple-filter-policy.md) — the base template pattern.
- [Collection Policy](collection-policy.md) — the registered policy.
- [Unit Tests](../testing/unit-tests.md) — validation matrix.
