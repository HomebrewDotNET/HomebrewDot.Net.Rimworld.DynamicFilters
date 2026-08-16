---
type: concept
title: Delegate Filtering Components
description: DelegateDynamicPolicy and DelegateDynamicFilter, the delegate-based implementations of IDynamicPolicy and IDynamicFilter used by preset policies such as BlocksWindmillPolicy.
tags: [filtering]
---

# Delegate Filtering Components

These two small classes (`Filtering/Components/DelegateDynamicPolicy.cs` and `DelegateDynamicFilter.cs`) implement the [filtering abstractions](concepts.md) with delegates, so a policy can be defined without writing a custom filter class. They are used by `BlocksWindmillPolicy` (see [Blocks Windmill Policy](../policies/blocks-windmill-policy.md)) and are the canonical pattern for code-defined policies.

## `DelegateDynamicPolicy<TScope, TItem>`

```csharp
public class DelegateDynamicPolicy<TScope, TItem> : IDynamicPolicy<TScope, TItem> where TScope : class
{
    public string Name { get; }
    public DelegateDynamicPolicy(string name, Func<TScope, IDynamicFilter<TScope, TItem>> filterFactory);
    public IDynamicFilter<TScope, TItem> GetFilter(TScope scope); // calls _filterFactory(scope)
}
```

- Guards: `Name` must be non-null/whitespace (`Guard.NotNullOrWhitespace`), `filterFactory` must be non-null.
- `GetFilter` invokes the factory **every call** — there is no caching. Unit tests pin this contract: `GetFilter_CalledMultipleTimes_InvokesFactoryEachTime` asserts three calls produce three invocations (see [Unit Tests](../testing/unit-tests.md)).

## `DelegateDynamicFilter<TScope, TItem>`

```csharp
public class DelegateDynamicFilter<TScope, TItem> : IDynamicFilter<TScope, TItem> where TScope : class
{
    public TScope Scope { get; }
    public IDynamicPolicy<TScope, TItem> Policy { get; }
    public DelegateDynamicFilter(TScope scope, IDynamicPolicy<TScope, TItem> policy,
        Func<TScope, TItem, bool> filter,
        Func<TScope, IStateStore<TScope>, bool> update = null);
    public bool Filter(TItem item);            // _filterFunc(Scope, item)
    public bool Update(IStateStore<TScope> stateStore); // _updateFunc?.Invoke(...) ?? false
}
```

- Guards on `scope`, `policy`, and `filter` (null throws `ArgumentNullException`); `update` is optional.
- `Filter` always passes the constructor scope to the delegate (pinned by `Filter_ReceivesScopePassedToConstructor`).
- `Update` with no update delegate returns `false` (no state change); with a delegate it returns the delegate's result.

Example (from `BlocksWindmillPolicy.Activate`): the policy captures itself so the filter can delegate back to the static `BlocksWind(def)` check:

```csharp
policy = new DelegateDynamicPolicy<Map, ThingDef>(name, map =>
    new DelegateDynamicFilter<Map, ThingDef>(map, policy, (m, def) => BlocksWind(def)));
```

## Related pages

- [Filtering Concepts](concepts.md) — the interfaces implemented here.
- [Blocks Windmill Policy](../policies/blocks-windmill-policy.md) — the consumer.
- [Unit Tests](../testing/unit-tests.md) — `DelegateDynamicFilterTests`, `DelegateDynamicPolicyTests`.
