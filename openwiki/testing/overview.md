---
type: concept
title: Testing Overview
description: The two xUnit test layers of the mod — tests/Unit (pure logic, no game assemblies) and tests/Integration (indexing pipeline and registries against a stubbed Toolkit environment) — and when to run which.
tags: [testing]
---

# Testing Overview

The mod has two xUnit test projects under `tests/` (both `net472`):

- **Unit tests** — `tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj` (assembly name `HomebrewDot.Net.RimWorld.DynamicFilters.Tests`). Pure logic: delegate filter/policy mechanics, `ActivatedPolicies`, state store, policy template validation matrices, `BlocksWindmillPolicy` rule matrix, preset condition structure/behavior. No game assemblies are required beyond `Assembly-CSharp`/`UnityEngine.CoreModule` references, and no defs are loaded; `Map` instances cannot be constructed, so `MapPolicyManager` is only smoke-tested via reflection.
- **Integration tests** — `tests/Integration/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests.csproj` (assembly name `HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests`). Stands up the Toolkit indexing pipeline (schema, gatherers, snapshot orchestrator) and the mod's registries (`Policies`, `Templates`, `DynamicFilterPresets`) in a pure .NET environment using defs fabricated with `FormatterServices.GetUninitializedObject`; it does **not** boot RimWorld. Tests that need Unity stat initialization (e.g. `StatDefOf`) are guarded and skip.

## Frameworks and conventions

- xUnit 2.x (`xunit`, `xunit.runner.visualstudio`), Moq 4.x, `Microsoft.NET.Test.Sdk` (versions pinned in `Directory.Packages.props`).
- Categories via `[Trait("Category", "Unit")]` / `[Trait("Category", "Integration")]`.
- Integration suites that share the indexing pipeline are grouped with `[Collection("IndexingIntegration")]` and a shared class fixture (`DynamicFilterPresetsFixture`).
- Tests mutate static state (`DynamicFiltersToolkit.Policies`, `Templates`, `Toolkit.Indexing` orchestrator/schema handlers) and restore it in `Dispose()`; `TemplatesIntegrationTests` resets the private `_templates` field through reflection.

## How to run

Both projects reference game/Toolkit assemblies from fixed Windows paths (see the csproj `<Reference>` items; env overrides `RIMWORLD_VERSION`, `HARMONY_VERSION`, `RIMWORLD_ROOT`, `RIMWORLD_WORKSHOP_ROOT`, `RIMWORLD_HARMONY_ROOT` exist). Run each project directly:

```sh
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests.csproj
```

The integration project needs `Assembly-CSharp.dll`, `UnityEngine.IMGUIModule.dll`, `0Harmony.dll`, and the Toolkit DLL on disk; the unit project additionally references `UnityEngine.CoreModule.dll`. There is no single command that runs both with zero setup — treat the per-project commands above as the narrow validation for changes.

## Relation to the wiki's systems

- Preset behavior: pinned in unit tests (structure + evaluation via the real comparator pipeline) and integration tests (real collected defs through `Collecting.Build`) — see [Preset Conditions](../presets/conditions.md).
- Policy template validation: `SimpleFilterPolicyTests` / `ComplexFilterPolicyTests` matrixes — see [Simple Filter Policy](../policies/simple-filter-policy.md) and [Complex Filter Policy](../policies/complex-filter-policy.md).
- Policy mechanics: `DelegateDynamicFilterTests`/`DelegateDynamicPolicyTests`, `ActivatedPoliciesTests`, `MapPolicyManagerTests` — see [Filtering Concepts](../filtering/concepts.md).
- Indexing wiring: `ThingFilterIndexingIntegrationTests` / `ThingFilterMapPersistenceTests` — see [Thing Filter Gatherer](../filtering/thing-filter-gatherer.md).

## Related pages

- [Unit Tests](unit-tests.md)
- [Integration Tests](integration-tests.md)
