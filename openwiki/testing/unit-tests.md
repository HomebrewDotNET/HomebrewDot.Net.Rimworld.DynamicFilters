---
type: concept
title: Unit Tests
description: The tests/Unit xUnit suite covering delegate filter/policy mechanics, ActivatedPolicies, StateStore, template validation matrices, BlocksWindmillPolicy, MapPolicyManager smoke tests, and preset condition structure/behavior.
tags: [testing]
---

# Unit Tests

Project: `tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj` (assembly `HomebrewDot.Net.RimWorld.DynamicFilters.Tests`, xUnit + Moq, `net472`). Suites are tagged `[Trait("Category", "Unit")]`. No game is booted: defs are mocked or built in memory, and because a real `Verse.Map` cannot be constructed, `MapPolicyManager` is only smoke-tested via reflection.

## Suite inventory

| Suite | File | What it proves |
|---|---|---|
| `DelegateDynamicPolicyTests` | `Filtering/Components/DelegateDynamicPolicyTests.cs` | Name/factory guards; `GetFilter` invokes the factory **every call** (no caching). |
| `DelegateDynamicFilterTests` | `Filtering/Components/DelegateDynamicFilterTests.cs` | Scope/policy/filter guards; `Filter` receives the constructor scope; `Update` returns delegate result or `false` without one. |
| `ActivatedPoliciesTests` | `Filtering/Models/ActivatedPoliciesTests.cs` | Constructor guards; default `Label`/`Title` = provider type name, `Description` empty; fluent `WithLabel`/`WithTitle`/`WithDescription` (with null guards); `AvailableFor` registration/chaining; `IsReadOnly`. |
| `StateStoreTests` | `State/Components/StateStoreTests.cs` | Instance assignment and null guard; root store (`Instance` null); `IDictionary` semantics (add/get/indexer/remove/clear/count/keys/values); child-store create/reuse/per-instance separation/destroy/re-create. |
| `SimpleFilterPolicyTests` | `Policies/SimpleFilterPolicyTests.cs` | Validation matrix with focused tests: `ValidateSettings_WithNull_ReturnsError`, `ValidateSettings_WithEmptyConditions_ReturnsError`, `ValidateSettings_WithInvalidPath_ReturnsRegexError` (dotted-path regex), `ValidateSettings_WithUnknownOperator_ReturnsError`, `Create_WithWrongType_Throws`; singleton `Instance`; namespaced `StorageKey`. |
| `ComplexFilterPolicyTests` | `Policies/ComplexFilterPolicyTests.cs` | Validation matrix for conditions + collection inclusions/exclusions, including `ValidateSettings_WithEmptyConfig_ReturnsError`, `ValidateSettings_WithConditionHavingEmptyOperator_ReturnsError`, `ValidateSettings_WithInvalidPropertyPath_ReturnsRegexError`, `ValidateSettings_WithUnknownOperator_ReturnsError`, `ValidateSettings_WithEmptyCollectionName_ReturnsError` (see [Complex Filter Policy](../policies/complex-filter-policy.md)). |
| `BlocksWindmillPolicyTests` | `Policies/BlocksWindmillPolicyTests.cs` | `BlocksWind` rule matrix: `blockWind`, plant `IsTree`, `treeCategory != None` (modded tree cases), `harvestTag` cases (see [Blocks Windmill Policy](../policies/blocks-windmill-policy.md)). |
| `DynamicFilterPresetsRottingTests` | `DynamicFilterPresetsRottingTests.cs` | Rotting condition structure (comp reference, `In` operator, `Rotting`+`Dessicated` stages) and behavior through the real comparator pipeline with `rotProgressInt` staged via reflection. |
| `DynamicFilterPresetsSmeltableTests` | `DynamicFilterPresetsSmeltableTests.cs` | Smeltable condition structure (2 conditions, OR-group) and behavior: non-stuff defs, smeltable/non-smeltable stuff, non-smeltable defs. |
| `DynamicFilterPresetsTechLevelTests` | `DynamicFilterPresetsTechLevelTests.cs` | Tech-level condition structure: 3 conditions, both guards (`Undefined` excluded, `ParentFaction` non-null), operator and `To` reference. |
| `MapPolicyManagerTests` | `Filtering/Components/MapPolicyManagerTests.cs` | Reflection-based smoke tests: `GetFor(null)` returns null; `TryGetActiveFilters` on a default state returns false; empty `GetActiveThingPolicyNames`/`GetActiveDefPolicyNames`. |

## Test doubles convention

- **Moq** mocks back `IDynamicPolicyProvider`/`IDynamicPolicyFilter`/`IDynamicPolicy` interfaces: `DelegateDynamicPolicyTests`, `DelegateDynamicFilterTests`, and `ActivatedPoliciesTests` are the Moq consumers.
- **`FormatterServices.GetUninitializedObject`** fabricates real game objects without running Unity-dependent constructors: `BlocksWindmillPolicyTests` and the Rotting/Smeltable behavioral tests build `ThingDef`/`Thing`/`CompRottable` instances this way.
- The **Rotting and Smeltable preset suites register Toolkit services from a static constructor** (`static DynamicFilterPresetsRottingTests()` / `static DynamicFilterPresetsSmeltableTests()`): because `Toolkit.ConfigureServices()` is internal to the Toolkit assembly, the static ctor calls `Services.Register<IReferenceType>(...)` for the six reference types (Indexed, Property, Value, Stat, Comp, Self) and `Services.Register<IOperatorType>(...)` for every operator alias (`EqualsOperatorType.Aliases`, ... `MatchOperatorType.Aliases`, plus the `In`, `Contains`, `InThingCategory` default names). A new behavioral preset test must replicate this registration before touching the comparator pipeline.

## Run

```sh
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj
```

Requires `Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, `0Harmony.dll`, and the Toolkit DLL on disk (paths in the csproj, env-overridable). Tests touching Unity-dependent statics are avoided by design.

## Related pages

- [Testing Overview](overview.md) — layers and run commands.
- [Integration Tests](integration-tests.md) — the def/indexing-backed suites.
- [Preset Conditions](../presets/conditions.md) — the factories the preset suites pin.
