---
type: concept
title: Policy Templates and Presets Configuration
description: IDynamicPolicyTemplate, the Preset base class for option-less singleton templates, and DelegatedPolicyPreset that wraps a configured policy as a read-only preset template.
tags: [configuration]
---

# Policy Templates and Presets Configuration

These abstractions (`Configuration/IDynamicPolicyTemplate.cs`, `Configuration/Templates/Preset.cs`, `Configuration/Components/DelegatedPolicyPreset.cs`) define how user-facing policy templates are described, validated, and instantiated. They are the contract behind the Templates tab UI (see [Templates Tab](../ui/templates-tab.md)) and the presets system (see [Presets Overview](../presets/overview.md)).

## `IDynamicPolicyTemplate`

```csharp
public interface IDynamicPolicyTemplate
{
    string StorageKey { get; }              // unique, namespaced; saved in settings to identify the template
    string GetTitle();                      // UI header
    string GetShortDescription();           // one-liner under the title
    string GetLongDescription(IExposable settings); // detailed description of the policy created with these settings
    bool Singleton { get; }                 // only one activated policy allowed per template
    void DrawSettings(Rect rect, ref IExposable settings); // renders template-specific inputs
    IEnumerable<string> ValidateSettings(IExposable settings); // error strings, empty when valid
    IDynamicPolicyProvider Create(IExposable settings);       // instantiate the provider
}
```

Key contract points:

- `StorageKey` should be namespaced (recommendation in the XML docs: `'MyMod.RangedWeaponFilter'`) to avoid collisions; it is what persists in `ActiveTemplates` and what `LoadActivatedTemplates` matches on game load.
- `DrawSettings` + `ValidateSettings` + `Create` form the create-policy flow driven by the UI: draw inputs → validate → confirm → `Create(settings)` → `TryActivateProvider`.
- `Singleton` guards duplicate activation in `LoadActivatedTemplates` and blocks re-creation in the Templates tab.

## `Preset` (abstract base)

`Configuration/Templates/Preset.cs` — a template that needs no user options:

- `Singleton` is always `true`.
- `DrawSettings` is a no-op; `ValidateSettings` returns an empty array.
- `Create(IExposable)` forwards to the abstract `Create()`.
- Implementors provide `StorageKey`, `Create()`, `GetLongDescription()`, `GetShortDescription()`, `GetTitle()`.
- Consumers: `BlocksWindmillPolicy` (see [Blocks Windmill Policy](../policies/blocks-windmill-policy.md)).

## `DelegatedPolicyPreset<T>`

`Configuration/Components/DelegatedPolicyPreset.cs` — a preset that configures *another* policy template (`T : IDynamicPolicyTemplate`) with pre-baked settings:

- Wraps `name`, `description`, the managed template `_policy`, and `_settings`.
- `StorageKey` = `"{_policy.StorageKey}::Preset::{_name}"` — distinct per preset name, so each preset is its own template entry while delegating creation to the wrapped template.
- `Create()` = `_policy.Create(_settings)`; `GetLongDescription()` = `_policy.GetLongDescription(_settings)`; `GetShortDescription()` = the preset's own description; `GetTitle()` = `"[Preset] {_name}"`.
- It overrides **none** of the base `Preset` option-less behavior: `DrawSettings` stays a no-op and `ValidateSettings` stays the always-empty `Array.Empty<string>()`. The delegated `_policy.ValidateSettings` is therefore never consulted — preset settings are pre-baked and never user-validated, so `UiExposableUtility.Validate` (see [UI Components](../ui/components.md)) always passes for a `DelegatedPolicyPreset<T>` and only `Create` is delegated.
- `DynamicFilterPresets.CreateSimple`/`CreatePreset` produce these (see [Presets Overview](../presets/overview.md)). When activated from the Templates tab they are read-only policies (the Policies tab hides edit/rename for read-only entries — see [Policies Tab](../ui/policies-tab.md)).

## Related pages

- [Templates Tab](../ui/templates-tab.md) — the UI flow built on this contract.
- [Mod Settings](../mod/settings.md) — persistence of `StorageKey` in `ActivatedTemplates`.
- [Mod Entry Point](../mod/entrypoint.md) — `Templates.All` registry and `LoadActivatedTemplates`.
