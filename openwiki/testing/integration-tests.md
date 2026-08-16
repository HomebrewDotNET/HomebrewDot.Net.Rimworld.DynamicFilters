---
type: concept
title: Integration Tests
description: The tests/Integration xUnit suite that stands up the Toolkit indexing pipeline and the mod's registries in pure .NET — preset def collection, policy/template registry lifecycle, and ThingFilter indexing persistence.
tags: [testing]
---

# Integration Tests

Project: `tests/Integration/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests.csproj` (assembly `HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests`, xUnit + Moq, `net472`). Suites are tagged `[Trait("Category", "Integration")]`.

These tests do **not** boot RimWorld. They stand up the Toolkit indexing pipeline (schema, gatherers, snapshot orchestrator) and the mod's registries in a pure .NET process, using defs fabricated with `FormatterServices.GetUninitializedObject` (which skips Unity-dependent constructors). Tests that need Unity stat initialization (e.g. `StatDefOf.Flammability` inside `ActivatePresets`) are guarded and skip on `TypeInitializationException`.

## Suites

| Suite | File | What it proves |
|---|---|---|
| `DynamicFilterPresetsFixture` | `DynamicFilterPresetsFixture.cs` | Shared class fixture: `ConfigureServices()`, a `TentityBool`/`TentityFloat` schema (via `ConfigureSchema += ConfigureTentitySchema`), `Indexing.StartIndexing(null, true)` **once**, defs pushed through the private `PushDef` helper (named `Test_Meat`, `Test_Metal`, `Test_Ingestible`, `Test_Food`, `Test_MealSimple/Lavish/Awful`, `Test_SnackJoy`, `Test_SnackRawTasty`, `Test_Medicine`, `Test_Apparel`, `Test_MeleeWeapon`, `Test_RangedWeapon`, `Test_Flammable`, `Test_NonFlammable`, `Test_Construction`, `Test_Explosive`, `Test_GenericItem`), `PushTentityData()` pushing `Tentity<bool>`/`Tentity<float>` instances, and `Indexing.Orchestrator.ForceSnapshot()` to materialize snapshots. `Dispose()` reverts the shared state: unsubscribes `ConfigureSchema`, forces a final snapshot, and calls `Collecting.ReloadDefaultComparator()` (all inside best-effort `InvokeSafe`). |
| `DynamicFilterPresetsIntegrationTests` | `DynamicFilterPresetsIntegrationTests.cs` (`[Collection("IndexingIntegration")]`, `IClassFixture<DynamicFilterPresetsFixture>`) | Each preset activated via `SimpleFilterPolicy.Instance.Create(settings)` + `Policies.TryActivateProvider`, then verified through the named collector (`Collecting.GetAllCollectors()[name]`): Meat matches/does-not-match, plus Metal, Ingestible, Food, Meal, GoodMeal, Snack, Medicinal, Apparel, Weapon, Melee/Ranged, Flammable, Construction, Explosive. |
| `DynamicFilterPresetsLifecycleIntegrationTests` | `DynamicFilterPresetsLifecycleIntegrationTests.cs` (`[Collection("IndexingIntegration")]`) | Factory outputs are non-empty; `DynamicFilterPresets_AddPresetProvider_ThenActivatePresets_CallsAllProviders` exercises `AddPresetProvider` + `ActivatePresets` and is guarded against `TypeInitializationException` whose `TypeName` contains `"StatDefOf"` (a pure .NET runner cannot initialize `StatDefOf.Flammability`); `DeactivateProvider` removes an active policy. Dispose deactivates every still-active policy. |
| `ThingFilterIndexingIntegrationTests` | `Indexing/ThingFilterIndexingIntegrationTests.cs` (`[Collection("IndexingIntegration")]`) | `EnsureTable`/`EnsureGatherer` run without throwing on a clean state; schema configuration callback fires (self-contained `Database`). Dispose **nulls the shared static state** (`Toolkit.Indexing.Orchestrator = null`, `Toolkit.Indexing.Manager = null`). |
| `ThingFilterMapPersistenceTests` | `Indexing/ThingFilterMapPersistenceTests.cs` (`[Collection("IndexingIntegration")]`) | Reproduces the `EnableStorageFiltering` wiring (`EnsureTable`, `Include<Map>` indexer, push gatherer) and asserts the pushed `Map` metadata (latest value) lands on the indexed `ThingFilter` via the Include change tracker. Dispose nulls `Orchestrator`/`Manager` and unsubscribes its `ConfigureSchema` handler. |
| `PoliciesIntegrationTests` | `Policies/PoliciesIntegrationTests.cs` | `TryActivateProvider`: new name true + context captured; duplicate without `deactivateExisting` false; duplicate with `deactivateExisting: true` replaces; `DeactivateProvider` removes active/does not throw when absent; `ActivePolicies` contains name; read-only/label behavior. Providers are Moq `Mock<IDynamicPolicyProvider>`; the private `CleanupAllPolicies` helper (called in ctor **and** `Dispose`) deactivates every active policy by name for isolation. |
| `TemplatesIntegrationTests` | `Templates/TemplatesIntegrationTests.cs` | `AddTemplate` appears in `Templates.All`; duplicates not re-added; empty registry; `All` ordered by `StorageKey`. Templates are Moq `Mock<IDynamicPolicyTemplate>`; `ResetTemplates()` (ctor + `Dispose`) reflects into the private static `_templates` `HashSet<IDynamicPolicyTemplate>` field and replaces it with a fresh empty set. |

## Run

```sh
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests.csproj
```

Requires `Assembly-CSharp.dll`, `UnityEngine.IMGUIModule.dll`, `0Harmony.dll`, and the Toolkit DLL on disk; the project embeds `AssemblyMetadataAttribute("RimworldLocation", ...)` for tooling.

## Related pages

- [Testing Overview](overview.md) — layers and run commands.
- [Unit Tests](unit-tests.md) — the no-indexing suites.
- [Thing Filter Gatherer](../filtering/thing-filter-gatherer.md) — the wiring `ThingFilterMapPersistenceTests` reproduces.
- [Presets Overview](../presets/overview.md) — the catalog the preset suites exercise.
