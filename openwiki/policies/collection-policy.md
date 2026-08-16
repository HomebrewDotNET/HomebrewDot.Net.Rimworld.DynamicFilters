---
type: concept
title: Collection Policy
description: CollectionPolicy, the policy that turns a named Toolkit collector into map-scoped filters for both ThingDefs and Things, including the per-map collection building, version-based update tracking, and ref-counted disposal.
tags: [policies]
---

# Collection Policy — `CollectionPolicy`

`CollectionPolicy` (`Policies/Components/CollectionPolicy.cs`) is the concrete policy every filter template activates: it implements both `IDynamicPolicy<Map, ThingDef>` and `IDynamicPolicy<Map, Thing>` for the same name, backed by the named Toolkit collector that the template's `Provider.Activate` built. This file also contains the private `Filter<T>` dynamic filter implementation.

## Policy level

```csharp
public class CollectionPolicy : IDynamicPolicy<Map, ThingDef>, IDynamicPolicy<Map, Thing>, IDisposable
```

- `_name` — the policy name, identical to the backing collection name.
- `internal int _filterTracker` — reference count of live def-filter instances.

**`GetFilter(Map)` for `Thing` (map-scoped):** builds a per-map collection named `"{map.GetUniqueLoadID()}.{_name}"` that filters the named base collection to items whose indexed `Map` equals the scope:

```csharp
Toolkit.Collecting.Build(mapCollectionName,
    x => x.Compare.Indexed(nameof(Map)).With.Equal(scope)
          .CollectFromCollection<ICollectionBuilder, Thing>(_name));
```

then returns `new Filter<Thing>(mapCollectionName, scope, this)`. Thing filters are therefore map-scoped (a `Thing` instance belongs to one map).

**`GetFilter(Map)` for `ThingDef` (def-level):** defs are global, so no extra map filtering is applied — it increments `_filterTracker` and returns `new Filter<ThingDef>(_name, scope, this)`.

**`Dispose()`** removes the backing collection `_name` only when `_filterTracker <= 0` (i.e. when no def-filter instances remain). Deactivation of a provider therefore disposes the map's filters; the last def filter disposed drops the collection (see [Map Policy Manager](../filtering/map-policy-manager.md)).

## `Filter<T>` (nested dynamic filter)

- `Update(IStateStore<Map>)`: resolves the collector by `_collectionName` each call (re-binding if it changed — `isNew`); for `SnapshotCollector<T>` it returns `true` only when `snapshotCollector.Version != _lastCollectionVersion` (version change → caller rewrites allow-lists); for non-snapshot collectors it always returns `true` (assumed always updated — no versioning).
- `Filter(T item)`: `collection?.Contains(item) ?? false` — inclusion is a set-membership test against the collector.
- `Dispose()`: for ThingDef filters decrements `_policy._filterTracker`; for Thing filters removes the per-map collection.

This is the update-model contract that `MapPolicyManager.MaintainActivePolicies` relies on: a changed snapshot version triggers a full allow-list rewrite; unchanged versions skip the expensive loop (see [Map Policy Manager](../filtering/map-policy-manager.md)).

## Related pages

- [Simple Filter Policy](simple-filter-policy.md) / [Complex Filter Policy](complex-filter-policy.md) — the templates that activate `CollectionPolicy`.
- [Filtering Concepts](../filtering/concepts.md) — `ICollectionPolicy` and the filter lifecycle.
