---
type: concept
title: Simple Filter Policy
description: SimpleFilterPolicy, the user-configurable template that filters ThingDefs or Things by a list of property/stat/comp conditions, including settings, condition model, validation, UI editing, and provider activation.
tags: [policies]
---

# Simple Filter Policy — `SimpleFilterPolicy`

`SimpleFilterPolicy` (`Policies/SimpleFilterPolicy.cs`) is the most flexible built-in template: a policy that includes every item whose properties match a list of `SimpleFilterPolicyCondition`s. It backs most of the built-in presets (see [Presets Overview](../presets/overview.md)).

## Template surface

- Singleton instance: `SimpleFilterPolicy.Instance` (private ctor).
- `StorageKey` = `"{DynamicFiltersToolkit.ModId}.SimpleFilterPolicy"` (namespaced; unit test asserts it contains the mod id).
- `Singleton` = `false` — players may create any number of simple filter policies.
- `GetTitle()` = `"Simple Filter Policy"`; `GetShortDescription()` = "Filter for matching thing(defs) based on specified conditions on their properties."
- `GetLongDescription(settings)` renders a human summary of the conditions (`ConditionDef.GroupToString`), prefixed "Match all ThingDefs / Things that satisfy the following conditions:", or "No conditions defined. This filter will match no ThingDefs." for empty lists.

## Settings model

`SimpleFilterPolicySettings : IExposable`:

- `bool ThingDef = true` — `true` filters `Verse.ThingDef`s; `false` filters `Verse.Thing`s (instance-level).
- `List<SimpleFilterPolicyCondition> Conditions` — the rule list.

`SimpleFilterPolicyCondition : IExposable` wraps a `ConditionDefConfig` (from the Toolkit's comparing model) as its editable state:

- `Config` — all editable fields (property path `CompareDefault`/`CompareType`, operator, value).
- `Condition` — the runtime `ConditionDef`: the cached static def when constructed via `FromDef(ConditionDef)`, otherwise `Config.ToConditionDef()`.
- `IsOr` (default false = AND) — combines this condition with the next using OR instead of AND; shown as an AND/OR toggle in the UI for all but the last row.
- `Inverted` (default false) — matches the negation of the underlying comparison.
- Factories: `FromConfig(ConditionDefConfig)` and `FromDef(ConditionDef)`; `ExposeData` deep-scrubs `Config`.

## Validation

`ValidateSettings` rejects:

- wrong settings type / null (yields an error),
- empty condition list ("At least 1 condition should be defined"),
- per condition: empty property path; property path not matching `DynamicFiltersToolkitConstants.Policy.PropertyPathRegex`; empty operator; operator not registered in `Toolkit.Services.GetAllNamed<IOperatorType>()`.

Group-only conditions (`ConditionDef` with nested `Conditions` and no `With`, e.g. produced by `ConditionBuilder` `.Group(...)`) skip leaf validation — this is how the smeltable preset's grouped condition passes validation.

Unit tests pin this matrix (`SimpleFilterPolicyTests`): invalid paths, unknown operators, empty lists, wrong settings type all yield errors; `Instance` is a singleton; `StorageKey` contains the mod id (see [Unit Tests](../testing/unit-tests.md)).

## UI

`DrawSettings(Rect, ref IExposable)` renders:

- a "ForThingDef" checkbox (toogles `ThingDef`),
- a scrollable condition list: each row shows the condition summary, an AND/OR toggle (except the last row), an `E` edit button (opens `ConditionDefEditorWindow` with the row's config; note the edit window currently discards changes — the callback is empty, so the condition is effectively read-only in this build), and an `X` delete button,
- an "Add Condition" button opening `ConditionDefEditorWindow` with a callback that appends `SimpleFilterPolicyCondition.FromConfig(config)`.

The panel is hosted by the Templates and Policies tabs (see [Templates Tab](../ui/templates-tab.md) and [Policies Tab](../ui/policies-tab.md)).

## Provider activation

`Create(IExposable settings)` returns a `Provider` (private nested class) that in `Activate(name, context)`:

1. Ensures the right Toolkit indexing surface: `Toolkit.Indexing.Def.EnsureGatherer()`/`Def.Thing.EnsureTable()` for ThingDef policies; `Toolkit.Indexing.Thing.EnsureGatherer()`/`Thing.EnsureTable()` for Thing policies.
2. Builds a named collection via `Toolkit.Collecting.Build(name, x => ...)`: each condition is added with `x.CompareFrom(condition.Condition)`, and the collection is a snapshot collection over the ThingDef or Thing table (the "th" way: `CollectFromSnapshot(...)`).
3. Registers the activation context label ("Simple Filter") and description from `GetLongDescription`.
4. Registers `context.AvailableFor<Map, ThingDef>(new CollectionPolicy(name))` or `<Map, Thing>(...)`.

`Deactivate` is a no-op; the collection and policy services are cleaned up through `ActivatedPolicies.Dispose` and `CollectionPolicy.Dispose` when the provider is deactivated (see [Collection Policy](collection-policy.md) and [Filtering Concepts](../filtering/concepts.md)).

## Related pages

- [Presets Overview](../presets/overview.md) — presets built on this template.
- [Preset Conditions](../presets/conditions.md) — how conditions are built programmatically.
- [Collection Policy](collection-policy.md) — the policy registered on activation.
- [Complex Filter Policy](complex-filter-policy.md) — the richer sibling template.
