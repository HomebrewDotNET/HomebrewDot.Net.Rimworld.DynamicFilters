---
type: concept
title: Mod Entry Point
description: DynamicFiltersToolkit, the Mod subclass that owns the mod lifecycle, settings hooks, the Templates and Policies static registries, the Indexing.ThingFilter wiring, and the storage-filtering enable/disable flow.
tags: [mod, entrypoint]
---

# Mod Entry Point — `DynamicFiltersToolkit`

`DynamicFiltersToolkit` (`DynamicFiltersToolkit.cs`) is the `Verse.Mod` subclass (assembly `HomebrewDot.Net.Rimworld.DynamicFilters`, namespace `HomebrewDot.Net.Rimworld`). RimWorld instantiates it from `About/About.xml` metadata; it is the composition root of the mod.

## Statics and singletons

- `ModId` = `"homebrewdot.net.rimworld.dynamicfilters"` — also the Harmony patch ID.
- `Harmony` — internal `HarmonyLib.Harmony` instance created with `ModId`; all patches in the mod use it.
- `Instance` — set in the constructor; accessing before construction throws `ArgumentNullException`.
- `Settings` — lazy `GetSettings<DynamicFiltersToolkitSettings>()` (see [Mod Settings](settings.md)).
- Private static flags: `_storageFilteringEnabled`, `_policiesLoadedFromSettings`, `_presetsActivated`.

## Constructor and lifecycle wiring

The constructor sets `Instance`, creates `DynamicFiltersSettingsUi`, and calls `ConfigureServices()`, which registers two hooks through `Toolkit.Hooks.Manager`:

1. `Changed` (the mod's own settings-change trigger, `byte.MaxValue` priority): toggles `EnableStorageFiltering()` / `DisableStorageFiltering()` and calls `SetPresets(Settings.EnablePresets)`.
2. `OnGameLoadedTrigger`: reapplies `SetPresets` and storage filtering, then lazily restores policies via `Policies.LoadActivatedTemplates(Settings.ActiveTemplates)`.

`DoSettingsWindowContents(Rect)` tracks the open `Dialog_ModSettings` (calling `_settingsUi.OnSettingsDialogOpened()` on first sight), lazily loads activated templates once, then draws `_settingsUi.Draw(inRect)`. `OpenPoliciesSettings()` (internal, used by the Policies toolbar button) either switches the already-open dialog to the Policies tab or opens a new `Dialog_ModSettings` with the tab preselected — see [Settings UI](../ui/settings.md).

## Storage filtering enable/disable

`EnableStorageFiltering()` (idempotent via `_storageFilteringEnabled`):

1. `Indexing.ThingFilter.EnsureGatherer()` + `EnsureTable()` — wires the ThingFilter table and the [ThingFilterGatherer](../filtering/thing-filter-gatherer.md) into the Toolkit indexing pipeline.
2. `StoragePolicyMapPatcher.ApplyPatches()` and `BetterWorkbenchManagementSupport.ApplyPatches()` — Harmony prefixes that enforce policies and render policy bars (see [Storage Policy Map Patcher](../storage/storage-policy-map-patcher.md) and [BWM integration](../integration/better-workbench-management.md)).
3. Registers the three always-available templates: `BlocksWindmillPolicy.Instance`, `SimpleFilterPolicy.Instance`, `ComplexFilterPolicy.Instance`.
4. Enables metadata trackers: `Toolkit.Indexing.Thing.TrackHitPointPercentage()`, `TrackModId()`, `TrackMap()`; builds indexers for `ThingFilter` on `ToolkitConstants.Thing.Map`, `DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey` (string), and `StorageKey` (object).
5. `Toolkit.Indexing.ReloadOrchestration()`.

`DisableStorageFiltering()` removes only the two patcher patch sets (`StoragePolicyMapPatcher.RemovePatches()`, `BetterWorkbenchManagementSupport.RemovePatches()`); templates and metadata trackers stay registered. This asymmetry matters: toggling the setting off stops enforcement but does not unregister templates.

`SetPresets(bool)` activates presets once (`DynamicFilterPresets.ActivatePresets()`) when enabled. Disabling does **not** deactivate them — the log message states "Presets will remain active until the game is restarted." (see [Presets Overview](../presets/overview.md)).

## `Templates` registry

`DynamicFiltersToolkit.Templates` is a static nested class holding `HashSet<IDynamicPolicyTemplate>`. `All` returns templates ordered by `StorageKey`; `AddTemplate(template)` adds idempotently (verbose log). Templates are the user-facing configuration surface for creating policies — see [Policy Templates and Presets Configuration](../configuration/templates.md). Registration happens in `EnableStorageFiltering()` (the three core templates), in `DynamicFilterPresets.CreatePreset` (one delegated preset per built-in preset), and from other mods via the public API.

## `Policies` registry

`DynamicFiltersToolkit.Policies` is the runtime registry of active policies, backed by `HashSet<IDynamicPolicyProvider> _availablePolicies` (advertised providers) and `Dictionary<string, ActivatedPolicies> _activePolicies` (activated by name).

- `ActivePolicies` / `ActivePoliciesInfo` — sorted names / sorted `ActivatedPolicies` records for the UI.
- `AddProvider(IDynamicPolicyProvider)` — registers a provider so its policies become available (currently informational; activation is what registers filters).
- `TryActivateProvider(name, provider, deactivateExisting, isReadOnly)` — rejects duplicates unless `deactivateExisting`; creates `ActivatedPolicies(name, provider, isReadOnly)`, calls `provider.Activate(name, context)`, stores it, and triggers `OnDynamicPolicyActivated` (see [Filtering Concepts](../filtering/concepts.md)).
- `DeactivateProvider(name)` — triggers `OnDynamicPolicyDeactivated`, calls `provider.Deactivate(disposeAction)` exactly once (guarded by `disposeCalled`), disposes `ActivatedPolicies` (which unregisters its `IDynamicPolicy` services), and removes the entry.
- `RenameProvider(oldName, newName)` — guarded, in order: (1) no active provider under `oldName` → warn + `false`; (2) read-only policy → warn + `false`; (3) case-insensitive same-name (`string.Equals(oldName, newName, OrdinalIgnoreCase)`) → `true` no-op; (4) `newName` already active → warn + `false`; (5) no `ActiveTemplates` entry with `PolicyName == oldName` → warn + `false`; (6) no template in `Templates.All` matching the entry's `StorageKey` → warn + `false`. Otherwise it deactivates the old provider, renames the template entry to `newName`, recreates the provider from the same settings via `template.Create(settings)`, and activates it under the new name. If the new activation fails, it rolls back by restoring `PolicyName = oldName` and re-activating a fresh `template.Create(settings)` under the old name, then returns `false`. Persists via `WriteSettings()`.
- `LoadActivatedTemplates(IEnumerable<ActivatedTemplates>)` — the game-load restore path: for each persisted entry, resolves the template by `StorageKey` (missing template → logs error and marks `LoadFailed = true`), skips duplicate activations of singleton templates (a second entry sharing a singleton `StorageKey` also marks `LoadFailed = true`), and activates inside `Invoking.Safe` — `LoadFailed` is pre-set to `true` before the invocation and cleared only on success, so both missing templates, duplicate singletons, and activation exceptions surface as failed entries. See [Mod Settings](settings.md) for the persisted shape.

## `Indexing.ThingFilter`

Static helper for the Toolkit database table of `Verse.ThingFilter`:

- `TableName` = `"ThingFilter"`; `EnsureTable()` subscribes a schema builder (`WithTable<Verse.ThingFilter>(TableName)`); `EnsureGatherer()` subscribes `ThingFilterGatherer.Instance` to the snapshot orchestrator; `GetTable()` / `GetCurrentTable()` return snapshot/live tables; `ConfigureTable(Action<ITableBuilder<ThingFilter>>)` composes custom schema configuration with `EnsureTable()`.

## `DynamicFiltersToolkitConstants`

Read-only constants (`DynamicFiltersToolkitConstants.cs`):

- `Policy.PropertyPathRegex` = `^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$` — validates dotted property paths in conditions (used by `SimpleFilterPolicy`/`ComplexFilterPolicy` validation).
- `ThingFilter.StorageIdKey` = `IndexMetadataKey<string>.Get("DynamicFilters.StorageId")` and `ThingFilter.StorageKey` = `IndexMetadataKey<object>.Get("DynamicFilters.Storage")` — the index metadata keys attached to every gathered `ThingFilter` (see [ThingFilterGatherer](../filtering/thing-filter-gatherer.md)).

## Related pages

- [Mod Settings](settings.md) — the settings model and persistence.
- [Filtering Concepts](../filtering/concepts.md) — the abstractions the registries operate on.
- [Storage Policy Map Patcher](../storage/storage-policy-map-patcher.md) — what `EnableStorageFiltering` enables.
