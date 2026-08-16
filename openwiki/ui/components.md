---
type: concept
title: Shared UI Components
description: TemplatePolicyEditorPanel (shared read-only/editing layout for template settings), PolicyNamePromptWindow (name + overwrite popup), and UiExposableUtility (Scribe-based clone and validation helper).
tags: [ui]
---

# Shared UI Components

The settings tabs share three components in `UI/Settings/Tabs/` (namespace `HomebrewDot.Net.Rimworld.UI.Settings.Tabs`).

## `TemplatePolicyEditorPanel`

Shared renderer for template-backed policy editors, used by both [Templates Tab](templates-tab.md) and [Policies Tab](policies-tab.md).

- `DrawReadOnly(rect, title, description, emptyMessage)` — draws a menu section with the title and a scrollable description (scrollbar when the natural text height exceeds 40% of the panel height).
- `Draw(rect, template, title, description, ref settings, validationErrors, emptyMessage)` returns `DrawResult` (`HasTemplate`, `ButtonsHeight` = 34f):
  1. Title, then a scrollable description (cap 30% of height).
  2. A boxed settings rect where `template.DrawSettings(settingsRect.ContractedBy(8f), ref settings)` renders the template-specific inputs.
  3. Validation errors as red `- {error}` lines below.
- `DrawResult.HasTemplate` is false when `template == null` (the Policies tab renders the panel without a template when the policy's template cannot be resolved).

## `PolicyNamePromptWindow`

Modal `Window` (520×220) that collects a **policy name** and an **overwrite existing** checkbox, then calls back `Func<string, bool, string>` (returns an error string, or null to close).

- Properties: `closeOnClickedOutside`, `doCloseX`, `absorbInputAroundWindow`, `forcePause`; no close button.
- Built-in validation: empty/whitespace name → "Policy name is required."; otherwise delegates to the callback and displays its error.
- Used by the Templates tab save flow and the Policies tab rename flow; its initial name suggestion comes from `template.GetTitle()` (create) or the current policy name (rename).

## `UiExposableUtility`

Static helpers for settings objects (`IExposable`):

- `Validate(template, settings)` → `template?.ValidateSettings(settings)` as a string array (empty when valid).
- `Clone(source)` → deep-clones an `IExposable` through `Scribe`: writes the object with `Scribe_Deep.Look` into `{SaveDataFolder}/HomebrewedDynamicFilters/UiTemp/template-settings-clone.xml`, reads it back, and deletes the file; returns the original on any failure.

## Related pages

- [Settings UI](settings.md) — the shell; [Templates Tab](templates-tab.md) and [Policies Tab](policies-tab.md) — consumers.
- [Policy Templates and Presets Configuration](../configuration/templates.md) — the `IDynamicPolicyTemplate` surface these components drive.
