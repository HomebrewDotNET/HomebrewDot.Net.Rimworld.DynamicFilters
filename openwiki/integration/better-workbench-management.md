---
type: concept
title: Better Workbench Management Integration
description: The soft BWM bridge (BetterWorkbenchManagementSupport) that indexes the per-bill "Count Additional" output filter, hides the thing policy bar on def-only filters, and applies def policies to BWM's per-def product counting via a reflection-based prefix.
tags: [integration]
---

# Better Workbench Management Integration — `BetterWorkbenchManagementSupport`

`BetterWorkbenchManagementSupport` (`Patches/BetterWorkbenchManagementSupport.cs`, namespace `HomebrewDot.Net.Rimworld.Patches`) integrates the **Better Workbench Management** (BWM, `falconne.BWM`) "Count Additional" output-filter panel with dynamic storage policies. It is a soft dependency: every BWM type and member is resolved by reflection, and nothing is patched when BWM is not loaded. Activation is gated on `ToolkitConstants.Mods.BetterWorkbenchManagement.IsLoaded` (see [Mod Entry Point](../mod/entrypoint.md) — `EnableStorageFiltering` calls `ApplyPatches`, `DisableStorageFiltering` calls `RemovePatches`).

## Why this integration exists

The BWM "Count Additional" panel edits a **per-bill** `ThingFilter` stored on BWM's `ExtendedBillData.ProductAdditionalFilter`. Unlike every other filter the toolkit indexes, BWM counts products by iterating `ProductAdditionalFilter.AllowedThingDefs` directly instead of calling `ThingFilter.Allows(Thing)`. Consequences:

- **Thing policies cannot function** on this filter — nothing calls the thing-level path.
- **Def policies need a dedicated hook** to affect the count, because the def allow-list written by `MaintainActivePolicies` is not consulted by BWM's counting loop.
- The panel normally edits a **working copy** of the filter, which would not be indexed and would never resolve the policy bars.

The class addresses all four points (see the class doc comment): it makes the panel operate on the live indexed instance, indexes the filter, hides the meaningless thing policy bar, and applies active def policies to the count through a prefix on BWM's counting helper.

## Reflection surface

Resolved once in `ResolveTypes()` from the `ToolkitConstants.Mods.BetterWorkbenchManagement` type-name constants; all must resolve or the integration disables itself with a warning log.

| Toolkit constant member | Resolved target |
|---|---|
| `MainTypeName` | BWM main type (static `Instance` property, `GetExtendedBillDataStorage()` method) |
| `ExtendedBillDataStorageTypeName` | storage type with `GetExtendedDataFor(bill)` |
| `ExtendedBillDataTypeName` | type holding the `ProductAdditionalFilter` field |
| `DialogThingFilterTypeName` | the filter dialog (`filter`, `extendedBill`, `reOpenWindow` fields) |
| `CountProductsDetourTypeName` | type with the `CountProducts(Bill_Production, ThingDef, bool)` method |

Also resolved: `Dialog_BillConfig.bill` (used to resolve the bill from the dialog's `reOpenWindow`).

## Patches

Installed by `ApplyPatches()` on `DynamicFiltersToolkit.Harmony`; removed by `RemovePatches()` (guarded by `_patchesApplied`):

| Target | Patch | Effect |
|---|---|---|
| `Dialog_ThingFilter` ctor (`(ExtendedBillData, Window)`) | `Postfix_Dialog_ThingFilter_Constructor` | When the bill already has an output filter, swaps the dialog's working copy for the **live indexed instance** so the policy bar resolves and edits apply to the stored filter. When there is no filter yet, keeps the working copy but indexes it immediately so the bar is available before commit. Either way, registers the displayed filter in `DefOnlyFilters`. |
| `Dialog_ThingFilter.PreClose` | `Postfix_Dialog_ThingFilter_PreClose` | When the dialog closes and BWM cleared the stored filter (emptied def list), destroys the previously displayed instance from the index and registries. |
| `CountProducts` | `Prefix_CountProducts` | Applies the active def policy to per-def counts (below). |

## `Prefix_CountProducts`

BWM counts products per def; this prefix lets a def policy veto non-default products:

1. `defaultProduct` (the bill's default product) is **always counted** — the prefix returns immediately.
2. Resolves the bill's output filter via `BillToFilter` (or reflection), the map's `MapPolicyManager` via `MapPolicyManager.GetFor(bill.Map)`, and `manager.TryGetActiveFilters(filter, ..., out defFilter, out defFilterInverted)`.
3. When a def filter is active and `defFilter.Filter(productThingDef)` fails (with inversion applied), sets `__result = 0` and returns `false` (skip original). Otherwise returns `true` so BWM's own count stands.

## Registries and gatherer integration

- `BillToFilter` (`Dictionary<Bill_Production, ThingFilter>`) — output filter keyed by bill; used by `Prefix_CountProducts` and lifecycle cleanup.
- `DefOnlyFilters` (`HashSet<ThingFilter>`) — the def-only filters on which the thing policy bar is hidden. `IsDefOnlyFilter(ThingFilter)` is consulted by the `ThingFilterUI` policy-bar prefix (`StoragePolicyMapPatcher.Prefix_ThingFilterUI_DoThingFilterConfigWindow`, see [Storage Policy Map Patcher](../storage/storage-policy-map-patcher.md)).
- `PushProductAdditionalFilter(Bill, Map)` / `DestroyProductAdditionalFilter(Bill, Map)` — public entry points called from the [ThingFilterGatherer](../filtering/thing-filter-gatherer.md) scan and its bill lifecycle patches (`BillStack.AddBill`/`Delete`). The filter is indexed with storage id `{bill.GetUniqueLoadID()}_ProductAdditional` via `ThingFilterGatherer.PushFilter`/`DestroyFilter`, and the registries are kept in sync.

## Related pages

- [Storage Policy Map Patcher](../storage/storage-policy-map-patcher.md) — the `IsDefOnlyFilter` consumer and the policy bars.
- [ThingFilterGatherer](../filtering/thing-filter-gatherer.md) — where the output filter is indexed.
- [Mod Entry Point](../mod/entrypoint.md) — `ApplyPatches`/`RemovePatches` lifecycle.
