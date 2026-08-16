---
type: concept
title: Build and Deploy
description: "How the DynamicFilters mod is compiled, packaged, and deployed: csproj layout for RimWorld 1.6 and Harmony 1.5, env-var overrides, post-build rsync sync to the Mods folder, checked-in assemblies, test projects, and the OpenWiki GitHub Actions workflow."
tags: [build, deploy]
---

# Build and Deploy

## Solution and project layout

The solution `HomebrewDot.Net.RimWorld.DynamicFilters.sln` contains three projects:

| Project | Path | Type |
|---|---|---|
| `HomebrewDot.Net.Rimworld.DynamicFilters` | `src/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.Rimworld.DynamicFilters.csproj` | mod class library, `net472` |
| `HomebrewDot.Net.RimWorld.DynamicFilters.Tests` | `tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj` | xUnit unit tests, `net472` |
| `HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests` | `tests/Integration/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests.csproj` | xUnit integration tests, `net472` |

Central package versions are managed in `Directory.Packages.props` (`BenchmarkDotNet 0.12.1`, `Microsoft.NET.Test.Sdk 16.11.0`, `Moq 4.20.72`, `xunit 2.4.2`, `xunit.runner.visualstudio 2.4.5`). `Directory.Build.props` only silences `NU1507` (duplicate package assets) so projects can override versions.

## Mod project properties (`HomebrewDot.Net.Rimworld.DynamicFilters.csproj`)

- `TargetFramework` = `net472`, `AssemblyName` = `HomebrewDot.Net.Rimworld.DynamicFilters`, `RootNamespace` = `HomebrewDot.Net.Rimworld`, `Nullable` disabled, `LangVersion` = `latestmajor`.
- `OutputPath` = `..\..\$(RimworldVersion)\Assemblies` — with the default `RimworldVersion` of `1.6`, assemblies land in `1.6/Assemblies/`, which is what RimWorld loads from the mod folder (the checked-in `1.6/Assemblies/HomebrewDot.Net.Rimworld.DynamicFilters.dll`/`.pdb` are those build outputs).
- Environment-variable overrides (all have defaults): `RIMWORLD_VERSION`, `HARMONY_VERSION` (default `1.5`), `RIMWORLD_ROOT` (default `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`), `RIMWORLD_WORKSHOP_ROOT`, `RIMWORLD_MANAGED`, `RIMWORLD_HARMONY_ROOT`, `RIMWORLD_TOOLKIT_ROOT` (default `../../../HomebrewDot.Net.Rimworld.Toolkit/$(RimworldVersion)/Assemblies`), `RIMWORLD_MODS_ROOT`, `DYNAMICFILTERS_MOD_TESTING_FOLDER_NAME` (default `Homebrewed Dynamic Filters - DEV`), `PUBLISHED_FILE_ID_FILE_NAME`.
- References (all `Private=false` except where noted): `Assembly-CSharp.dll` and UnityEngine modules (Core, IMGUI, TextRendering) from the RimWorld managed folder; `0Harmony.dll` from the workshop folder; `HomebrewDot.Net.Rimworld.Toolkit.dll` from `$(ToolkitRoot)`.
- `InternalsVisibleTo` for both test assemblies (`HomebrewDot.Net.RimWorld.DynamicFilters.Tests`, `HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests`).
- A `Compile Remove` for the (non-existent) `Policies\Template\**` glob, and an empty `UI\Components\` folder item.

### Post-build sync target

`SyncModContentToModsFolderForTesting` runs `AfterTargets=Build` only when `$(RimworldModsRoot)` exists. It uses **rsync** (skips with a message if `rsync` is not on PATH, or if the repository path is a UNC network path) to copy:

- `About/About.xml` → `<Mods>/<ModTestingFolderName>/About/About.xml`
- `$(RimworldVersion)/` (the built assemblies) → mod folder
- `About/DevPublishedFileId.txt` → `About/PublishedFileId.txt` (when present)
- `Defs/`, `Patches/`, `Languages/`, `Textures/`, `Sounds/` (when present)

This is how a dev build lands in the RimWorld `Mods` folder for in-game testing.

## Test projects

Both test projects target `net472` and reference the mod project plus `HomebrewDot.Net.Rimworld.Toolkit.dll` (hint path `..\..\..\..\HomebrewDot.Net.Rimworld.Toolkit\1.6\Assemblies\HomebrewDot.Net.Rimworld.Toolkit.dll`, `Private=true`). The unit project hard-codes `Assembly-CSharp.dll` and `UnityEngine.CoreModule.dll` under `C:\Program Files (x86)\Steam\...`; the integration project additionally references `UnityEngine.IMGUIModule.dll` and embeds an `AssemblyMetadataAttribute` `RimworldLocation` pointing at `$(RimworldRoot)`.

**Validation**: `dotnet test tests/Unit/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.Tests.csproj` and the equivalent for the integration project. Both require RimWorld, Harmony, and Toolkit assemblies on disk; tests touching `StatDefOf` or other Unity-dependent statics are guarded (see [Testing Overview](../testing/overview.md)).

## Packaging for players

The distributable mod folder is the repository root layout as RimWorld expects it: `About/About.xml`, `Defs/`, `1.6/Assemblies/`, and any optional content folders. `About.xml` declares dependencies on Core (`ludeon.rimworld`), Harmony (`brrainz.harmony`), and the Homebrewed Toolkit (`homebrewdot.net.rimworld.toolkit`, GitHub download URL), and `loadAfter` `falconne.BWM` so the Better Workbench Management integration (see [Better Workbench Management Integration](../integration/better-workbench-management.md)) can resolve types.

## OpenWiki update workflow

`.github/workflows/openwiki-update.yml` runs daily at 08:00 UTC (and on `workflow_dispatch`):

1. `actions/checkout` with `fetch-depth: 0` (full history so `openwiki code --update` can diff against the last documented commit).
2. Node 22; `npm install --global openwiki@0.3.1 mermaid@11.16.0 jsdom@29.1.1`.
3. `openwiki code --update --print` with `OPENWIKI_PROVIDER=openrouter` and model `z-ai/glm-5.2`.
4. `peter-evans/create-pull-request` opens PR `openwiki/update` adding `openwiki`, `AGENTS.md`, `CLAUDE.md`, and the workflow file.

`AGENTS.md` (in-repo, hand-edited) tells agents the wiki is an optional evidence index: source and tests are authoritative, and the workflow refreshes generated pages, so hand-edits to generated pages are discouraged.
