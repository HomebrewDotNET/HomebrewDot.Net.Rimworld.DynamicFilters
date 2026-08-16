---
type: concept
title: Quickstart
description: Entry point to the Dynamic Filters Toolkit wiki — a map of the pages, the core concepts and APIs, and a task-routing table from engineering intent to source, tests, and validation.
tags: [overview]
---

# Quickstart — Homebrewed Dynamic Filters (RimWorld mod)

This wiki documents the RimWorld 1.6 mod **Homebrewed Dynamic Filters** (packageId `homebrewdot.net.rimworld.dynamicfilters`, assembly `HomebrewDot.Net.Rimworld.DynamicFilters`). The mod lets players attach **dynamic filter policies** to the game's `Verse.ThingFilter` objects (stockpiles, storage buildings and storage groups, bill ingredient filters, outfits, food restrictions, pens, wind turbines, and Better Workbench Management "Count Additional" output filters). A policy is a named rule — created from a **template** in the settings UI or shipped as a read-only **preset** — that is applied continuously to a chosen filter.

The mod is built on the sibling `HomebrewDot.Net.Rimworld.Toolkit` DLL (external dependency), which supplies indexing, condition/collector engines, services, and the hook system. Start with [Architecture Overview](architecture/overview.md) for the end-to-end flow.

## Wiki map

| Area | Pages |
|---|---|
| Architecture | [Overview](architecture/overview.md), [Build and Deploy](architecture/build-and-deploy.md) |
| Mod core | [Entry Point & registries](mod/entrypoint.md), [Settings model](mod/settings.md) |
| Filtering | [Concepts](filtering/concepts.md), [Delegate components](filtering/delegate-components.md), [Map Policy Manager](filtering/map-policy-manager.md), [Thing Filter Gatherer](filtering/thing-filter-gatherer.md) |
| Policies | [Simple Filter Policy](policies/simple-filter-policy.md), [Complex Filter Policy](policies/complex-filter-policy.md), [Collection Policy](policies/collection-policy.md), [Blocks Windmill Policy](policies/blocks-windmill-policy.md) |
| Presets | [Overview + catalog](presets/overview.md), [Condition factories](presets/conditions.md) |
| Configuration | [Templates contract](configuration/templates.md) |
| State | [State Store](state/state-store.md) |
| Enforcement | [Storage Policy Map Patcher](storage/storage-policy-map-patcher.md), [BWM integration](integration/better-workbench-management.md) |
| UI | [Settings UI](ui/settings.md), [Templates Tab](ui/templates-tab.md), [Policies Tab](ui/policies-tab.md), [Shared UI Components](ui/components.md) |
| Testing | [Overview](testing/overview.md), [Unit tests](testing/unit-tests.md), [Integration tests](testing/integration-tests.md) |

## Core concepts in one paragraph

Templates (`IDynamicPolicyTemplate`) describe configurable policies; the player configures one in the **Templates tab** and commits it, which persists an `ActivatedTemplates` entry and activates a provider through `DynamicFiltersToolkit.Policies.TryActivateProvider`. The provider builds a named Toolkit **collection** (`Toolkit.Collecting.Build`) from conditions and registers a `CollectionPolicy` for `Map`/`ThingDef` or `Map`/`Thing` via `Toolkit.Services`. Per map, `MapPolicyManager` (a `MapComponent`) instantiates those filters, persists the filter→policy association, and rewrites def allow-lists on snapshot-version changes. At runtime, `StoragePolicyMapPatcher` prefixes `ThingFilter.Allows(Thing)` to enforce thing/def filters (with inversion), and `ThingFilterUI`'s config window gets policy bars. Presets are pre-baked read-only templates (`DelegatedPolicyPreset`) activated by `DynamicFilterPresets.ActivatePresets()` when `EnablePresets` is on.

## Task routing

| Intent / change area | Start at | Owning source | Focused tests | Narrow validation |
|---|---|---|---|---|
| Add a new built-in preset | [Presets Overview](presets/overview.md) | `DynamicFilterPresets.cs` (+ `PresetPatches.cs` for mod-gated) | `DynamicFilterPresets*Tests` (unit), `DynamicFilterPresetsIntegrationTests` | `dotnet test` on the unit csproj |
| Add a new policy template | [Configuration templates](configuration/templates.md) | `Configuration/IDynamicPolicyTemplate.cs`, `Policies/*.cs` | `SimpleFilterPolicyTests`/`ComplexFilterPolicyTests` (validation matrix) | unit test csproj |
| Add a filter target (new ThingFilter source) | [Thing Filter Gatherer](filtering/thing-filter-gatherer.md) | `Filtering/Components/ThingFilterGatherer.cs` | `ThingFilterMapPersistenceTests`, `ThingFilterIndexingIntegrationTests` | integration test csproj |
| Change enforcement semantics | [Storage Policy Map Patcher](storage/storage-policy-map-patcher.md) | `Storage/Models/StoragePolicyMapPatcher.cs`, `MapPolicyManager` | `MapPolicyManagerTests`, integration suites | integration test csproj |
| Change policy lifecycle/registry | [Mod Entry Point](mod/entrypoint.md) | `DynamicFiltersToolkit.cs` (`Policies`/`Templates`) | `PoliciesIntegrationTests`, `TemplatesIntegrationTests`, `ActivatedPoliciesTests` | integration test csproj |
| BWM integration | [BWM integration](integration/better-workbench-management.md) | `Patches/BetterWorkbenchManagementSupport.cs` | (none in-repo; manual with BWM loaded) | build + manual |
| UI flows | [Settings UI](ui/settings.md) → tabs | `UI/Settings/*` | — | build + manual |

## Extension seams

- **New preset**: call `DynamicFilterPresets.AddPresetProvider(activator => ...)` (from `[StaticConstructorOnStartup]`, like `PresetPatches`) or add a `CreateSimple(...)` call in `ActivatePresets()`.
- **New template**: implement `IDynamicPolicyTemplate` (or subclass `Preset`) and register via `DynamicFiltersToolkit.Templates.AddTemplate`; activation must call `context.AvailableFor<Map, Thing>`/`<Map, ThingDef>` so `MapPolicyManager` picks it up.
- **New soft mod integration**: mirror `BetterWorkbenchManagementSupport` (reflection + conditional patches gated on `ToolkitConstants.Mods.*.IsLoaded`).

## Backlog

None. Every substantial component is documented; the external Toolkit DLL is described only at the dependency boundary used by this mod.
