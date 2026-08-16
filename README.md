# Homebrewed Dynamic Filters for RimWorld

## Description

Homebrewed Dynamic Filters is a RimWorld 1.6 mod that attaches dynamic filter policies to the game's `Verse.ThingFilter` objects: stockpiles, storage buildings and storage groups, bill ingredient filters, outfits, food restrictions, pens, wind turbines, and Better Workbench Management "Count Additional" output filters.

A policy is a named rule ("all metallic stuff", "everything that rots", "all defs from mod X") created from a template in the mod settings, or activated as a read-only preset. It is applied continuously to the chosen filter and can be combined with the vanilla filter.

The mod is built on the sibling [Homebrewed Toolkit](https://github.com/HomebrewDotNET/HomebrewDot.Net.Rimworld.Toolkit) assembly, which provides the indexing, condition and collector engines, services, and hooks.

## Installation

### Players

Install like any other mod and enable "Homebrewed Dynamic Filters" in the mod list. Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) and the Homebrewed Toolkit.

Open the mod settings and toggle what you want:

- "Enable storage policies and filters" — the master switch for dynamic storage policies.
- "Enable policy presets" — activates the built-in presets as read-only templates.
- "Enable special thing filter presets" — (shown while presets are enabled) adds a preset for every special thing filter the game loads, including filters from other mods.
- "Show Policies button in toolbar" — adds a toolbar button that opens the Policies tab.

### Building from source

Requires the .NET SDK and RimWorld 1.6 with the Harmony and Toolkit assemblies on disk. Build the mod project:

```
dotnet build src/HomebrewDot.Net.RimWorld.DynamicFilters/HomebrewDot.Net.RimWorld.DynamicFilters.csproj
```

Assemblies land in `1.6/Assemblies/`. When rsync is available, the build also syncs content to a dev folder in RimWorld's `Mods` directory. Paths default to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` and can be overridden with environment variables such as `RIMWORLD_VERSION`, `RIMWORLD_ROOT`, and `RIMWORLD_HARMONY_ROOT`.

## Usage

### In game

1. Enable the features you want in the mod settings.
2. In the Templates tab, pick a template and configure it. Presets appear here as `[Preset] name` templates and are read-only.
3. Activate the policy, then attach it to a filter (stockpile, storage, bill, outfit, food restriction, pen, or wind turbine). A policy bar shows in the filter config window and is enforced on every item check.
4. The Policies tab lists active policies with edit, rename, and delete.

Built-in templates: Simple Filter Policy, Complex Filter Policy, and Blocks Windmill. Built-in presets cover resources, food, meals, drinks, corpses, medical and surgical items, stuff categories, perishability, tech level, equipment (weapons, apparel, tools, shields), damaged equipment (tattered and worn out apparel/weapons), and more. An opt-in setting adds a preset for every special thing filter the game loads — the stockpile "Allow ..." checkboxes (allow fresh, allow colonist corpses, allow smeltable, allow clean apparel, and the rest, including filters added by other mods) — each using the exact same check as the game's own filter and taking precedence over the built-in presets that cover the same ground (for example allow rotten replaces the Rotting preset). Like all policies, activating one registers its collection, so the Complex Filter Policy can include or exclude it.

### Adding presets from another mod

Register a preset provider and create the preset with `DynamicFilterPresets.CreateSimple`:

```csharp
using HomebrewDot.Net.Rimworld;
using Verse;

[StaticConstructorOnStartup]
public static class MyModPresets
{
    static MyModPresets()
    {
        DynamicFilterPresets.AddPresetProvider(activator =>
        {
            DynamicFilterPresets.CreateSimple("My Mod Stuff", "Filters all defs from my mod",
                new[] { DynamicFilterPresets.CreateModFilterCondition("mynamespace.myid") },
                thingDef: true);
        });
    }
}
```

Condition factories in `DynamicFilterPresets` cover properties, stats, comps, mod IDs, tech level, rotting, and smeltable.

### Adding a custom template

A template with user input implements `IDynamicPolicyTemplate` and is registered with `DynamicFiltersToolkit.Templates.AddTemplate`:

```csharp
using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Filtering;
using UnityEngine;
using Verse;

public class RangedWeaponFilterTemplate : IDynamicPolicyTemplate
{
    public string StorageKey => "MyMod.RangedWeaponFilter";
    public bool Singleton => false;

    public string GetTitle() => "Ranged Weapon Filter";
    public string GetShortDescription() => "Includes ranged weapons above a configured range";
    public string GetLongDescription(IExposable settings) => "Includes all ranged weapons with a range above the configured value";
    public void DrawSettings(Rect rect, ref IExposable settings) { }
    public IEnumerable<string> ValidateSettings(IExposable settings) => Array.Empty<string>();
    public IDynamicPolicyProvider Create(IExposable settings) => new RangedWeaponFilterProvider(settings);
}

DynamicFiltersToolkit.Templates.AddTemplate(new RangedWeaponFilterTemplate());
```

## Contributing

Not accepting direct contributions right now. Feel free to fork.

## License

Licensed under Apache License 2.0. See [LICENSE.md](LICENSE.md).
