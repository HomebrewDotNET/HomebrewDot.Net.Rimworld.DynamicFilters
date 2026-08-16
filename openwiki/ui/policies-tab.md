---
type: concept
title: Policies Tab
description: PoliciesUiTab, the settings tab listing active non-read-only policies with edit, rename, and delete flows backed by the Policies registry and the persisted ActiveTemplates entries.
tags: [ui]
---

# Policies Tab — `PoliciesUiTab`

`PoliciesUiTab` (`UI/Settings/Tabs/PoliciesUiTab.cs`) is the tab for **inspecting and editing active policies** (see [Mod Entry Point](../mod/entrypoint.md) — `Policies` registry). It is one of the three tabs hosted by [Settings UI](settings.md).

## Layout

Two columns split at `ListWidthRatio = 0.42f`:

- **Left** — "Active Policies": search box and a scrollable list of `Policies.ActivePoliciesInfo` **excluding read-only entries** (`GetFilteredPolicies()` filters `Where(p => !p.IsReadOnly)`), ordered by name. Rows show the policy name plus its `Label` when different. Selecting a row calls `LoadPolicyInfo`, which resolves the `ActiveTemplates` entry by `PolicyName` and then the template from `Templates.All` by `StorageKey` so the right-side editor can render template settings.
- **Right** — the selected policy: read-only detail or editing mode via the shared [TemplatePolicyEditorPanel](components.md).

## Read-only mode

Title (`policy.Title ?? policy.Name`) and description (template long description when resolved, else `policy.Description`). Buttons:

- **Edit** (only when the template resolved) — enters editing mode by setting `_editingPolicyName` and calling `LoadPolicyEditorState(policyName)`, which resolves the `ActiveTemplates` entry by `PolicyName` and the template from `Templates.All` by `StorageKey` (same resolution as `LoadPolicyInfo`), then loads `activeTemplate.Settings` as the working settings.
- **Rename** (only when the template resolved) — opens `PolicyNamePromptWindow` via `OpenRenamePrompt`; the callback validates empty name, same-name (`New name must differ`, compared case-insensitively), and duplicate names against `Policies.ActivePolicies`, then calls `Policies.RenameProvider(old, new)` and reports "Failed to rename policy. Check the log for details." on failure.
- **Delete** — `ConfirmWindow`, then `DeletePolicy`: `Policies.DeactivateProvider(name)`, removes the `ActiveTemplates` entry with an **exact (case-sensitive) `PolicyName` match** (`RemoveAll(x => x.PolicyName == policyName)`), `WriteSettings()`.

## Editing mode

`_editorPanel.Draw(...)` with `_editingTemplate` and the policy's persisted `Settings` as working settings. Buttons: **Cancel** (reloads read-only info via `CancelEditing`), **Save** (only when the template resolved and policy not read-only), and **Delete**.

`RequestSavePolicyEdits` (the Save handler) first guards read-only policies and a null `_editingTemplate`, then runs `UiExposableUtility.Validate(_editingTemplate, _workingSettings)` — on any error it shows the validation errors and stops; only zero errors open a `ConfirmWindow` leading to `CommitPolicyEdits`.

`CommitPolicyEdits` on confirm:

1. `_editingTemplate.Create(_workingSettings)`; `Policies.TryActivateProvider(policyName, provider, deactivateExisting: true)` — recreates the provider from the edited settings (failure surfaces a validation error).
2. Updates the matching `ActiveTemplates` entry's `Settings` and calls `WriteSettings()`.

Because the activation context is a fresh provider under the same name, the per-map filters are replaced through the `OnDynamicPolicyDeactivated`/`OnDynamicPolicyActivated` hooks (see [Map Policy Manager](../filtering/map-policy-manager.md)).

## Related pages

- [Settings UI](settings.md) — the shell hosting the tab.
- [Templates Tab](templates-tab.md) — creation flow.
- [Mod Entry Point](../mod/entrypoint.md) — `Policies.TryActivateProvider`/`DeactivateProvider`/`RenameProvider`.
