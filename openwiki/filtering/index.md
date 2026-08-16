# Files

- [Filtering Concepts](concepts.md) - The core filtering abstractions of the mod: IDynamicPolicy, IDynamicFilter, ICollectionPolicy, IDynamicPolicyProvider with its fluent activation context, the ActivatedPolicies record, and the activation/deactivation triggers.
- [Delegate Filtering Components](delegate-components.md) - DelegateDynamicPolicy and DelegateDynamicFilter, the delegate-based implementations of IDynamicPolicy and IDynamicFilter used by preset policies such as BlocksWindmillPolicy.
- [Map Policy Manager](map-policy-manager.md) - MapPolicyManager, the per-map MapComponent that instantiates filters for active policies, manages the ThingFilter-to-policy association, maintains def allow-lists on ticks, and persists those associations across save/load.
- [Thing Filter Gatherer](thing-filter-gatherer.md) - ThingFilterGatherer, the Toolkit data gatherer that indexes every manageable ThingFilter (stockpiles, storage buildings and groups, bill ingredient filters, outfits, food restrictions, pens, wind turbines) with Storage/StorageId/Map metadata and keeps the index in sync via Harmony lifecycle patches.
