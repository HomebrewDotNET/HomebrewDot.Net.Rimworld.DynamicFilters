---
type: concept
title: Settings UI
description: The tabbed settings dialog shell (DynamicFiltersSettingsUi with Settings/Templates/Policies tabs), the SettingsUiTab checkboxes and failed-policy cleanup, and the optional Policies toolbar button (PoliciesButtonWorker + MainButtonDef).
tags: [ui]
---

# Settings UI — dialog shell, Settings tab, and the Policies button

The mod's settings surface lives in `UI/Settings/` (`HomebrewDot.Net.Rimworld.UI.Settings`):

- `DynamicFiltersSettingsUi` — the tab shell drawn inside RimWorld's `Dialog_ModSettings`.
- `IDynamicFiltersSettingsTab` — contract for a tab (`Title`, `Draw(Rect)`).
- `UI/Settings/Tabs/*` — the three tabs (see [Templates Tab](templates-tab.md), [Policies Tab](policies-tab.md)).
- `UI/PoliciesButtonWorker.cs` + `Defs/MainButtonDefs/PoliciesButton.xml` — the optional toolbar button.

## `DynamicFiltersSettingsUi` — shell and tab coordination

Constructed by the [Mod Entry Point](../mod/entrypoint.md) constructor. Holds three tabs in order: `SettingsUiTab`, `TemplatesUiTab`, `PoliciesUiTab`.

- `Draw(Rect)` renders a 30px tab bar (`Widgets.DrawMenuSection` + `ButtonInvisible`, highlight on the selected tab) then delegates the remaining rect to the selected tab.
- `OnSettingsDialogOpened()` — called from `DynamicFiltersToolkit.DoSettingsWindowContents` the first time it observes an open `Dialog_ModSettings`; applies a pending tab selection (from the Policies button) or resets to tab 0.
- `SelectPoliciesTab()` / `SelectPoliciesTabImmediately()` — used by `DynamicFiltersToolkit.OpenPoliciesSettings()` to preselect the Policies tab when opening, or switch immediately when the dialog is already open.

## `SettingsUiTab` — checkboxes and failed-policy cleanup

`Listing_Standard` with the persisted toggles (see [Mod Settings](../mod/settings.md)):

- "Enable storage policies and filters" → `Settings.EnableStorageFiltering`
- "Enable policy presets" → `Settings.EnablePresets`
- "Enable special thing filter presets" → `Settings.EnableSpecialThingFilterPresets` — only drawn while `EnablePresets` is on; the checkbox tooltip explains that it adds a preset per loaded special thing filter (vanilla and modded), that built-in presets duplicating a special thing filter (e.g. Rotting vs allow rotten) make way for it, that activating a preset registers a collection the Complex Filter Policy can include or exclude, and that enabling takes effect immediately
- "Show Policies button in toolbar" → `Settings.ShowPoliciesButton`

Then `DrawFailedTemplateCleanup(listing)` renders a "Remove failed policies (n)" button: it collects every `ActiveTemplates` entry whose `LoadFailed` flag is set (entries `Policies.LoadActivatedTemplates` could not restore — e.g. a template from a removed mod) into `failedTemplates`, labels the button "Remove failed policies (none found)" when the list is empty, otherwise shows a `ConfirmWindow` (built by `BuildRemovalMessage` with the affected policy names, sorted by `PolicyName`), and on confirm calls `RemoveFailedTemplates(failedTemplates)`, which removes all `LoadFailed` entries from `ActiveTemplates`, calls `WriteSettings()`, and posts a `Messages.Message`. See [Mod Settings](../mod/settings.md) for the flag semantics.

## Policies toolbar button

`Defs/MainButtonDefs/PoliciesButton.xml` defines `MainButtonDef HomebrewDot_Policies` (label "policies", order 100, `validWithoutMap` true) with worker class `HomebrewDot.Net.Rimworld.UI.PoliciesButtonWorker`:

- `Visible => DynamicFiltersToolkit.Settings.ShowPoliciesButton`.
- `Activate()` → `DynamicFiltersToolkit.Instance.OpenPoliciesSettings()` (opens the mod settings with the Policies tab selected, or switches the open dialog).

## Related pages

- [Mod Entry Point](../mod/entrypoint.md) — `DoSettingsWindowContents`, `OpenPoliciesSettings`.
- [Templates Tab](templates-tab.md) / [Policies Tab](policies-tab.md) — the other two tabs.
- [Shared UI Components](components.md) — `TemplatePolicyEditorPanel`, `PolicyNamePromptWindow`, `UiExposableUtility`.
