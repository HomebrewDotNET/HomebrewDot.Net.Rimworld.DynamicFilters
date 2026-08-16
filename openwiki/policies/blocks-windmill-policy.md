---
type: concept
title: Blocks Windmill Policy
description: BlocksWindmillPolicy, the singleton preset template that filters all defs which block wind turbines, covering blockWind, PlantProperties.IsTree, and modded treeCategory-only trees such as Alpha Bees hive trees.
tags: [policies, presets]
---

# Blocks Windmill Policy — `BlocksWindmillPolicy`

`BlocksWindmillPolicy` (`Policies/BlocksWindmillPolicy.cs`) is a built-in singleton preset that filters every `ThingDef` that can block a wind turbine. Unlike the condition-based templates it is a hard-coded delegate policy (`Preset` + `IDynamicPolicyProvider`), registered in `EnableStorageFiltering()` as one of the three always-available templates (see [Mod Entry Point](../mod/entrypoint.md)).

## The rule: `BlocksWind(ThingDef)`

```csharp
public static bool BlocksWind(ThingDef def)
    => def.blockWind
       || (def.category == ThingCategory.Plant
           && def.plant != null
           && (def.plant.IsTree || def.plant.treeCategory != TreeCategory.None));
```

A def blocks wind when:

- `def.blockWind` is set (buildings, e.g. walls/trees marked by content), **or**
- it is a plant with `PlantProperties` that is a tree — vanilla trees set `IsTree` via `harvestTag`/`forceIsTree`; modded trees (e.g. Alpha Bees hive trees) only set a `TreeCategory` (`harvestTag` "Standard", no `forceIsTree`), which makes `IsTree` false but `treeCategory != None` true.

`MakePlant`-based unit tests cover exactly these cases: `blockWind` true, vanilla tree (harvestTag "Wood"), tree-category-only hive tree, `forceIsTree` true, non-tree plant (treeCategory None), plant without plant properties, and non-plant items (see [Unit Tests](../testing/unit-tests.md)).

## Provider shape

- `StorageKey` = `"{DynamicFiltersToolkit.ModId}.BlocksWindmillPolicy"`.
- `Singleton` = `true` (inherited from `Preset`) — only one "Blocks Windmill" policy can exist; the Templates tab shows it as already active and blocks re-creation (see [Templates Tab](../ui/templates-tab.md)).
- `Activate(name, context)` builds a `DelegateDynamicPolicy<Map, ThingDef>` whose factory returns a `DelegateDynamicFilter<Map, ThingDef>` calling `BlocksWind(def)` (see [Delegate Filtering Components](../filtering/delegate-components.md)); context is labeled/titled/described with `GetTitle()`/`GetShortDescription()`.
- `Create()` returns `this` (the policy itself is the provider — a shared singleton), so `Deactivate` is a no-op.
- Descriptions: title "Blocks Windmill", short description "Filters all definitions that can block a windmill."

## Related pages

- [Presets Overview](../presets/overview.md) — where presets fit in.
- [Delegate Filtering Components](../filtering/delegate-components.md) — the delegate policy pattern used here.
- [Configuration templates](../configuration/templates.md) — the `Preset` base contract.
