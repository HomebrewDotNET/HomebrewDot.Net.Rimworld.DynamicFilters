---
type: concept
title: Templates Tab
description: TemplatesUiTab, the settings tab where the player picks a registered policy template, configures its settings through IDynamicPolicyTemplate.DrawSettings, validates, names, and commits a new policy.
tags: [ui]
---

# Templates Tab — `TemplatesUiTab`

`TemplatesUiTab` (`UI/Settings/Tabs/TemplatesUiTab.cs`) is the tab for **creating** policies from the registered templates (see [Policy Templates and Presets Configuration](../configuration/templates.md)). It is one of the three tabs hosted by [Settings UI](settings.md).

## Layout

Two columns split at `ListWidthRatio = 0.42f`:

- **Left** — "Available Templates": a search box, a "Hide activated singleton templates" checkbox, and a scrollable list of `DynamicFiltersToolkit.Templates.All` ordered by title. Each row shows `GetTitle()` and `GetShortDescription()` (prefixed `(Active: {name})` when a singleton policy already exists for the template). Selecting a row resets the working settings.
- **Right** — the selected template: read-only mode or editing mode, rendered through the shared [TemplatePolicyEditorPanel](components.md).

## Read-only mode

Shows title + short description. If a singleton policy exists, a green "Already active as policy '{name}'." status replaces the buttons; otherwise an **Edit** button starts editing.

## Editing mode

`_editorPanel.Draw(...)` renders the template title, a scrollable `GetLongDescription(_workingSettings)`, the template's `DrawSettings(rect, ref _workingSettings)` inside a box, and any validation errors in red. Buttons:

- **Cancel** — discards working settings.
- **Save** — runs `UiExposableUtility.Validate(template, _workingSettings)`; on zero errors, blocks singleton templates that are already active, then opens `PolicyNamePromptWindow` (suggested name = `GetTitle()`) with an overwrite checkbox. The prompt callback (`SaveTemplatePolicy`) re-validates: empty name, singleton conflict (checked case-insensitively against the existing singleton policy name), and "A policy with this name already exists. Enable overwrite to replace it." (when the name matches an active policy case-insensitively and overwrite is off). Passing checks opens a `ConfirmWindow` with the long description.

`CommitTemplatePolicy` (on confirm) then:

1. `provider = template.Create(_workingSettings)`; `Policies.TryActivateProvider(policyName, provider, deactivateExisting: overwrite)`; failure sets a validation error and aborts.
2. Upserts `Settings.ActiveTemplates`: for singleton templates removes any entry with the same `StorageKey` (dedupe), otherwise removes entries whose `PolicyName` equals the new name **case-insensitively** (`StringComparison.OrdinalIgnoreCase`), then adds a new `ActivatedTemplates { StorageKey, PolicyName, Settings }`.
3. `WriteSettings()` and resets the editing state.

## Related pages

- [Settings UI](settings.md) — the shell hosting the tab.
- [Policies Tab](policies-tab.md) — editing/deleting the policies created here.
- [Policy Templates and Presets Configuration](../configuration/templates.md) — the contract the tab drives.
