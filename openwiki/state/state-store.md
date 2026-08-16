---
type: concept
title: State Store
description: IStateStore and StateStore, the generic key-value store with per-instance child stores used by dynamic filters to hold update state scoped to a Map.
tags: [state]
---

# State Store — `IStateStore<T>` / `StateStore<T>`

The state store (`State/IStateStore.cs`, `State/Components/StateStore.cs`) is a small generic dictionary that holds extra state on objects — the mechanism `IDynamicFilter.Update(IStateStore<TScope>)` uses to detect and react to changes in the filter's scope.

## `IStateStore<out T>`

```csharp
public interface IStateStore<out T> : IDictionary<string, object> where T : class
{
    T Instance { get; }                              // object the store hangs off; null for the root
    IStateStore<TChild> GetChildStore<TChild>(TChild instance) where TChild : class; // get-or-create
    bool DestroyChildStore<TChild>(TChild instance);  // remove a child store
}
```

## `StateStore<T>` implementation

- `StateStore<object>.Root` — static root store with no instance, used as the entry point.
- Stores values case-insensitively (`StringComparer.OrdinalIgnoreCase`).
- Child stores are keyed by instance reference in `_childStores`; `GetChildStore` returns the existing store or creates one per instance; `DestroyChildStore` removes it.
- Full `IDictionary<string, object>` implementation over `_state`.

## Usage in the mod

`MapPolicyManager` uses `StateStore<Map>.Root.GetChildStore(map)` as the store handed to every `filter.Update(...)` call (thing filters on ticks, def filters in `MaintainActivePolicies`). `DelegateDynamicFilter.Update` passes it to the update delegate unchanged (see [Map Policy Manager](../filtering/map-policy-manager.md) and [Delegate Filtering Components](../filtering/delegate-components.md)).

Unit tests (`StateStoreTests`) pin: instance assignment and null guard, root behavior (non-null root with null `Instance`), dictionary semantics (add/get/indexer/remove/clear/count/keys/values), and child-store semantics (create, same-instance reuse, per-instance separation, destroy, and re-create after destroy) — see [Unit Tests](../testing/unit-tests.md).

## Related pages

- [Filtering Concepts](../filtering/concepts.md) — `IDynamicFilter.Update` signature.
- [Map Policy Manager](../filtering/map-policy-manager.md) — the primary consumer.
