---
type: concept
title: Mod Settings
description: DynamicFiltersToolkitSettings, the ModSettings subclass that persists EnableStorageFiltering, EnablePresets, ShowPoliciesButton, and the ActiveTemplates list used to restore policies after a game load.
tags: [mod, settings]
---

# Mod Settings — `DynamicFiltersToolkitSettings`

`DynamicFiltersToolkitSettings` is a nested class of `DynamicFiltersToolkit` (`DynamicFiltersToolkit.cs`, nested in `DynamicFiltersToolkit` as `DynamicFiltersToolkit.DynamicFiltersToolkitSettings : ModSettings`). RimWorld persists it through `Scribe` when the mod saves, and it is accessible at runtime via `DynamicFiltersToolkit.Settings`.

## Persisted fields

| Field | Type | Default | Meaning |
|---|---|---|---|
| `EnableStorageFiltering` | `bool` | `true` | Master switch for dynamic storage policies and the filtering hooks. When false, `StoragePolicyMapPatcher` and `BetterWorkbenchManagementSupport` patches are removed (see [Mod Entry Point](entrypoint.md)). |
| `EnablePresets` | `bool` | `false` | Enables the built-in read-only presets (`DynamicFilterPresets.ActivatePresets()`). Disabling later does not deactivate already-activated presets until restart. |
| `EnableSpecialThingFilterPresets` | `bool` | `false` | Adds read-only presets for every loaded special thing filter (the stockpile "Allow ..." checkboxes, including modded ones). Only takes effect while `EnablePresets` is also enabled; the checkbox is only offered in the UI when presets are enabled. Enabling takes effect immediately. |
| `ShowPoliciesButton` | `bool` | `false` | Shows the Policies toolbar button (`PoliciesButtonWorker`, see [Settings UI](../ui/settings.md)). |
| `ActiveTemplates` | `List<ActivatedTemplates>` | empty | The persisted list of user-created/activated policies, used to restore them on game load and to drive the Policies tab. |

## `ExposeData`

```csharp
Scribe_Collections.Look(ref ActiveTemplates, nameof(ActiveTemplates), LookMode.Deep);
Scribe_Values.Look(ref EnablePresets, ..., defaultValue: false);
Scribe_Values.Look(ref EnableSpecialThingFilterPresets, ..., defaultValue: false);
Scribe_Values.Look(ref EnableStorageFiltering, ..., defaultValue: true);
Scribe_Values.Look(ref ShowPoliciesButton, ..., defaultValue: false);
if (Scribe.mode == LoadSaveMode.Saving)
    Toolkit.Hooks.Manager.Trigger(new Changed(this));
```

Saving raises the `Changed` trigger, which `ConfigureServices()` listens for (max priority) to apply setting changes live (see [Mod Entry Point](entrypoint.md)).

## `ActivatedTemplates` (persisted policy entry)

`ActivatedTemplates : IExposable` is the persisted record for one activated policy:

- `StorageKey` — which template created it (matches `IDynamicPolicyTemplate.StorageKey`, e.g. `homebrewdot.net.rimworld.dynamicfilters.SimpleFilterPolicy`).
- `PolicyName` — the unique, user-chosen name of the active policy.
- `Settings` (`IExposable`, deep-scrubbed via `Scribe_Deep`) — the template-specific settings (e.g. `SimpleFilterPolicySettings` or `ComplexFilterPolicySettings`).
- `LoadFailed` (internal, not persisted) — set when `Policies.LoadActivatedTemplates` could not restore this entry (missing template/mod or activation failure); the Settings tab offers a "Remove failed policies" cleanup that deletes entries with this flag (see [Settings UI](../ui/settings.md)).

`ExposeData` scrubs `StorageKey`, `PolicyName`, and `Settings` in that order.

## Consumption flow

1. **Save**: the UI (Templates/Policies tabs) mutates `ActiveTemplates` and calls `DynamicFiltersToolkit.Instance.WriteSettings()`; `ExposeData` persists and raises `Changed`.
2. **Load**: `DoSettingsWindowContents` or the `OnGameLoadedTrigger` hook calls `Policies.LoadActivatedTemplates(Settings.ActiveTemplates)` once (`_policiesLoadedFromSettings` guards double-loading), which recreates providers and activates them (see [Mod Entry Point](entrypoint.md)).
3. **UI**: `PoliciesUiTab` and `TemplatesUiTab` read `ActiveTemplates` to show singleton status, edit, rename, and delete policies; `SettingsUiTab` surfaces `LoadFailed` entries (see [Policies Tab](../ui/policies-tab.md), [Templates Tab](../ui/templates-tab.md)).

## `Changed` trigger

`DynamicFiltersToolkitSettings.Changed` is a plain trigger payload holding the settings instance. It is raised on save and consumed by `DynamicFiltersToolkit.ConfigureServices()` to apply `EnableStorageFiltering`/`EnablePresets` changes at runtime.

## Related pages

- [Mod Entry Point](entrypoint.md) — owner and hooks.
- [Policies Tab](../ui/policies-tab.md), [Templates Tab](../ui/templates-tab.md) — UI that mutates these settings.
- [Policy Templates and Presets Configuration](../configuration/templates.md) — the template contract behind `StorageKey`/`Settings`.
