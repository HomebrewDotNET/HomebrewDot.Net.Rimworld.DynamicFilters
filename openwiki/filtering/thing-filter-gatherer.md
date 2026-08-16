---
type: concept
title: Thing Filter Gatherer
description: ThingFilterGatherer, the Toolkit data gatherer that indexes every manageable ThingFilter (stockpiles, storage buildings and groups, bill ingredient filters, outfits, food restrictions, pens, wind turbines) with Storage/StorageId/Map metadata and keeps the index in sync via Harmony lifecycle patches.
tags: [filtering, indexing]
---

# Thing Filter Gatherer — `ThingFilterGatherer`

`ThingFilterGatherer` (`Filtering/Components/ThingFilterGatherer.cs`) is a singleton `IDataGatherer` (Toolkit indexing interface). It scans the game on save-load and pushes every `Verse.ThingFilter` the mod can manage into the Toolkit snapshot manager, tagged with the metadata the rest of the system needs: `DynamicFilters.Storage` (the owning object), `DynamicFilters.StorageId` (a stable unique string), and `ToolkitConstants.Thing.Map` (the map, where applicable).

It is wired in by `DynamicFiltersToolkit.Indexing.ThingFilter.EnsureGatherer()` during `EnableStorageFiltering()` (see [Mod Entry Point](../mod/entrypoint.md)).

## What gets indexed

| Source | StorageId | Notes |
|---|---|---|
| `Zone_Stockpile` (`zone.settings.filter`) | `zone.GetUniqueLoadID()` | from `map.zoneManager.AllZones` |
| `Building_Storage` (`GetStoreSettings().filter`) | `storageGroup != null ? storageGroup.GetUniqueLoadID() : building.GetUniqueLoadID()` | storage-group members share the group's filter and ID |
| Bill ingredient filters (`Bill.ingredientFilter`) | `bill.GetUniqueLoadID()` | on `Building_WorkTable.billStack` |
| BWM "Count Additional" output filter | `{bill}_ProductAdditional` | via `BetterWorkbenchManagementSupport.PushProductAdditionalFilter` (see [BWM integration](../integration/better-workbench-management.md)) |
| `CompAnimalPenMarker.AnimalFilter` | `{building}_Animals` | pen animal filter |
| `CompAutoCut.AutoCutFilter` | `{building}_AutoCut` | wind turbines and pen auto-cut (trees) |
| Outfit filters (`outfitDatabase.AllOutfits`) | `outfit.GetUniqueLoadID()` | no map metadata |
| Food restriction filters (`foodRestrictionDatabase.AllFoodRestrictions`) | `foodRestriction.GetUniqueLoadID()` | no map metadata |

Def-level and thing-level policies can both manage any of these; the metadata's `Map` value is what lets `MapPolicyManager.GetFor(map)` and the `ThingFilter.Allows` prefix resolve the right manager (filters without a map, e.g. outfits, are still indexed but map-scoped policy bars resolve through `ResolveMap` in the patcher, which looks up the indexed `Map` value).

## Scan

`Scan(Game)` iterates `game.Maps` (zones, colonist buildings: storage, work tables with bills, pens/auto-cut comps) and then `game.outfitDatabase` / `game.foodRestrictionDatabase`. Each push builds an `IndexMetadata` with the three keys and calls `SnapshotManager.Push(filter, ref metadata)` (the pen/auto-cut paths use `PushFilter` which passes `persistent: false`).

`PushFilter`/`DestroyFilter` (internal statics) are the reusable push/remove helpers; `DestroyPenMarkerFilters`/`DestroyAutoCutFilter` remove the pen and auto-cut entries.

## Harmony lifecycle patches

`Initialize(Game)` installs postfixes (unpatched by `Reset()`), all targeting `DynamicFiltersToolkit.Harmony`:

| Patched method | Postfix | Effect |
|---|---|---|
| `Zone.Deregister` | `Postfix_Zone_Deregister` | destroy stockpile filter |
| `Zone.PostRegister` | `Postfix_Zone_PostRegister` | push stockpile filter |
| `BillStack.Delete` | `Postfix_BillStack_Delete` | destroy bill ingredient filter (+ BWM output filter) |
| `BillStack.AddBill` | `Postfix_BillStack_AddBill` | push bill ingredient filter (+ BWM output filter) |
| `Building_Storage.Notify_SettingsChanged` | `Postfix_Building_Storage_Notify_SettingsChanged` | re-push storage filter |
| `Building_Storage.Destroy` | `Postfix_Building_Storage_Destroy` | destroy storage filter; for storage-group members never destroys the group's filter, only the member's own filter when it has settings separate from the group |
| `Building_Storage.SpawnSetup` | `Postfix_Building_Storage_SpawnSetup` | push on new construction; skips when `respawningAfterLoad` (initial `Scan` already indexed) |
| `CompAutoCut.PostSpawnSetup` | `Postfix_CompAutoCut_SpawnSetup` | push auto-cut filter (wind turbines and pen markers via base call); skips when `respawningAfterLoad` |
| `CompAnimalPenMarker.PostSpawnSetup` | `Postfix_CompAnimalPenMarker_SpawnSetup` | push pen animal filter; skips when `respawningAfterLoad` |
| `ThingComp.PostDeSpawn` | `Postfix_ThingComp_DeSpawn` | destroy pen marker / auto-cut filters |

The `respawningAfterLoad` skips prevent duplicate pushes during the initial load scan; the storage-group destroy guard (comment at `Postfix_Building_Storage_Destroy`) preserves the shared group filter while other members still exist.

## Indexer wiring

`EnableStorageFiltering()` also builds the `ThingFilter` indexers that turn this metadata into queryable columns:

- `ToolkitConstants.Thing.Map` (Include with `persistent: true`)
- `DynamicFilters.StorageId` (string, persistent)
- `DynamicFilters.Storage` (object, persistent)

`ThingFilterMapPersistenceTests` in the integration suite reproduces this exact wiring and asserts the map metadata survives pushes to the snapshot (see [Integration Tests](../testing/integration-tests.md)).

## Related pages

- [Mod Entry Point](../mod/entrypoint.md) — `Indexing.ThingFilter` wiring and `DynamicFiltersToolkitConstants`.
- [Map Policy Manager](map-policy-manager.md) — consumer of the StorageId/Map metadata.
- [Better Workbench Management Integration](../integration/better-workbench-management.md) — the per-bill output filter integration called from the scan and bill patches.
