# Files

- [Blocks Windmill Policy](blocks-windmill-policy.md) - BlocksWindmillPolicy, the singleton preset template that filters all defs which block wind turbines, covering blockWind, PlantProperties.IsTree, and modded treeCategory-only trees such as Alpha Bees hive trees.
- [Collection Policy](collection-policy.md) - CollectionPolicy, the policy that turns a named Toolkit collector into map-scoped filters for both ThingDefs and Things, including the per-map collection building, version-based update tracking, and ref-counted disposal.
- [Complex Filter Policy](complex-filter-policy.md) - ComplexFilterPolicy, the template that combines property conditions with inclusion and exclusion of other registered collections into a single CollectionDefConfig, with validation and provider activation.
- [Simple Filter Policy](simple-filter-policy.md) - SimpleFilterPolicy, the user-configurable template that filters ThingDefs or Things by a list of property/stat/comp conditions, including settings, condition model, validation, UI editing, and provider activation.
- [Special Thing Filter Presets](special-thing-filter-preset.md) - The worker-backed preset catalog that mirrors every loaded SpecialThingFilterDef (vanilla or modded) through the MatchesThingFilter operator, including duplicate filtering against built-in presets and the EnableSpecialThingFilterPresets gating.
