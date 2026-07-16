using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Filtering.Triggers;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.State;
using HomebrewDot.Net.Rimworld.State.Components;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Filtering.Components
{
    /// <summary>
    /// Manages policy filters scoped to a <see cref="Map"/>. This component is responsible for storing and updating filters that are associated with the map, and providing access to these filters for other components or systems that need to apply them.
    /// </summary>
    public class MapPolicyManager : MapComponent, IHook<OnDynamicPolicyActivated>, IHook<OnDynamicPolicyDeactivated>, IHook<OnGameTickTrigger>, IExposable
    {
        // Statics
        private static IStateStore<object> StateStore => StateStore<Map>.Root;
        private static readonly Dictionary<Map, MapPolicyManager> _instances = new Dictionary<Map, MapPolicyManager>();

        /// <summary>
        /// Gets the <see cref="MapPolicyManager"/> for the given <see cref="Map"/>, using an internal cache
        /// to avoid the O(n) linear scan in <see cref="Map.GetComponent{T}"/>.
        /// </summary>
        /// <param name="map">The map to get the manager for.</param>
        /// <returns>The manager if it exists; otherwise, null.</returns>
        internal static MapPolicyManager GetFor(Map map)
        {
            _instances.TryGetValue(map, out var instance);
            return instance;
        }

        // Fields
        private readonly object _lock = new object();
        private readonly Dictionary<string, IDynamicFilter<Map, Thing>> _thingFilters = new Dictionary<string, IDynamicFilter<Map, Thing>>();
        private readonly Dictionary<string, IDynamicFilter<Map, ThingDef>> _defFilters = new Dictionary<string, IDynamicFilter<Map, ThingDef>>();
        private Dictionary<ThingFilter, string> _filterToThingCache = new Dictionary<ThingFilter, string>();
        private Dictionary<ThingFilter, string> _filterToDefCache = new Dictionary<ThingFilter, string>();
        private Dictionary<ThingFilter, (IDynamicFilter<Map, Thing> Thing, bool ThingInverted, IDynamicFilter<Map, ThingDef> Def, bool DefInverted)> _filterCache = new Dictionary<ThingFilter, (IDynamicFilter<Map, Thing> Thing, bool ThingInverted, IDynamicFilter<Map, ThingDef> Def, bool DefInverted)>();
        private Dictionary<string, string> _storageToDefFilterMap = new Dictionary<string, string>();
        private Dictionary<string, string> _storageToInvertedDefFilterMap = new Dictionary<string, string>();
        private Dictionary<string, string> _storageToThingFilterMap = new Dictionary<string, string>();
        private Dictionary<string, string> _storageToInvertedThingFilterMap = new Dictionary<string, string>();


        /// <inheritdoc/>
        object IHook<OnDynamicPolicyActivated>.Owner => this;
        /// <inheritdoc/>
        bool IHook<OnDynamicPolicyActivated>.Once => false;
        /// <inheritdoc/>
        byte IHandler.Priority => byte.MinValue;
        /// <inheritdoc/>
        object IHook<OnDynamicPolicyDeactivated>.Owner => this;
        /// <inheritdoc/>
        bool IHook<OnDynamicPolicyDeactivated>.Once => false;
        /// <inheritdoc/>
        object IHook<OnGameTickTrigger>.Owner => this;
        /// <inheritdoc/>
        bool IHook<OnGameTickTrigger>.Once => false;

        /// <inheritdoc cref="MapPolicyManager"/>
        /// <param name="map">The map that this component is associated with.</param>
        public MapPolicyManager(Map map) : base(map)
        {
            _instances.Remove(map);
            _instances.Add(map, this);
        }
        
        /// <summary>
        /// Associates the given <see cref="ThingFilter"/> with the specified policy name, allowing it to be managed and updated according to that policy. This method checks if the policy is active on this map, and if the filter is properly indexed. If the filter is already managed by another policy, it will be unmanaged before being assigned to the new policy. Returns true if the filter is successfully managed with the policy, false otherwise.
        /// </summary>
        /// <param name="filter">The filter to be managed.</param>
        /// <param name="policyName">The name of the policy to manage the filter with.</param>
        /// <returns>True if the filter is successfully managed with the policy, false otherwise.</returns>
        public bool ManageWith(ThingFilter filter, string policyName, bool isForThing, bool inverted)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            policyName = Guard.NotNullOrWhitespace(policyName, nameof(policyName));
            if (!_thingFilters.ContainsKey(policyName) && !_defFilters.ContainsKey(policyName))
            {
                if (IsVerboseEnabled) LogVerbose($"Policy {policyName} is not active for map {map}, cannot manage filter {filter} with this policy");
                return false;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table == null)
            {
                if (IsVerboseEnabled) LogVerbose($"ThingFilter table is not available, cannot manage filter {filter} with policy {policyName}");
                return false;
            }
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                lock (_lock)
                {
                    var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                    if (string.IsNullOrWhiteSpace(storageId))
                    {
                        LogWarning($"Filter {filter} is not properly indexed (missing storage ID), cannot manage with policy {policyName}");
                        return false;
                    }

                    if (isForThing)
                    {
                        if (inverted)
                        {
                            _storageToInvertedThingFilterMap[storageId] = policyName;
                        }
                        else
                        {
                            _ = _storageToInvertedThingFilterMap.Remove(storageId);
                        }
                        if (_storageToThingFilterMap.TryGetValue(storageId, out var existingPolicy))
                        {
                            if (existingPolicy == policyName)
                            {
                                _filterCache.Clear();
                                if (IsVerboseEnabled) LogVerbose($"Filter {filter} is already managed by thing policy {policyName}, skipping");
                                return true;
                            }
                            else
                            {
                                Log($"Filter {filter} is already managed by thing policy {existingPolicy}, unmanaging it before assigning to policy {policyName}");
                                Unmanage(filter, isForThing);
                            }
                        }
                        _storageToThingFilterMap[storageId] = policyName;
                        _filterToThingCache[filter] = policyName;
                        Log($"Filter {filter} on {storageId} is now managed by thing policy {policyName}");
                    }
                    else
                    {
                        if (inverted)
                        {
                            _storageToInvertedDefFilterMap[storageId] = policyName;
                        }
                        else
                        {
                            _ = _storageToInvertedDefFilterMap.Remove(storageId);
                        }
                        if (_storageToDefFilterMap.TryGetValue(storageId, out var existingPolicy))
                        {
                            if (existingPolicy == policyName)
                            {
                                _filterCache.Clear();
                                if (IsVerboseEnabled) LogVerbose($"Filter {filter} is already managed by def policy {policyName}, skipping");
                                MaintainActivePolicies(true);
                                return true;
                            }
                            else
                            {
                                Log($"Filter {filter} is already managed by def policy {existingPolicy}, unmanaging it before assigning to policy {policyName}");
                                Unmanage(filter,isForThing);
                            }
                        }
                        _storageToDefFilterMap[storageId] = policyName;
                        _filterToDefCache[filter] = policyName;
                        Log($"Filter {filter} on {storageId} is now managed by def policy {policyName}");
                    }

                    _filterCache.Clear();
                    if(!isForThing)
                        MaintainActivePolicies(true);

                    return true;
                }
            }
            else
            {
                if (IsVerboseEnabled) LogVerbose($"Filter {filter} is not indexed, cannot manage with policy {policyName}");
                return false;
            }
        }
        /// <summary>
        /// Removes any association between the given <see cref="ThingFilter"/> and any policy that may be managing it. This method checks if the filter is indexed and currently managed by a policy, and if so, it removes the association and updates the internal state accordingly. If the filter is not indexed or not currently managed, it logs a verbose message and takes no action.
        /// </summary>
        /// <param name="filter">The filter to be unmanaged.</param>
        public void Unmanage(ThingFilter filter, bool isForThing)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            _filterToDefCache.Remove(filter);
            _filterToThingCache.Remove(filter);
            _filterCache.Remove(filter);
            if (table == null)
            {
                if (IsVerboseEnabled) LogVerbose($"ThingFilter table is not available, cannot unmanage filter {filter}");
                return;
            }
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                lock (_lock)
                {
                    var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                    if (!string.IsNullOrWhiteSpace(storageId))
                    {
                        if (isForThing)
                        {
                            if (_storageToThingFilterMap.TryGetValue(storageId, out var policyName))
                            {
                                _storageToThingFilterMap.Remove(storageId);
                                Log($"Filter {filter} is no longer managed by thing policy {policyName}");
                            }
                        }
                        else
                        {
                            if(_storageToDefFilterMap.TryGetValue(storageId, out var policyName))
                            {
                                _storageToDefFilterMap.Remove(storageId);
                                Log($"Filter {filter} is no longer managed by def policy {policyName}");
                            }
                        }
                        _filterCache.Remove(filter);
                    }
                    else
                    {
                        if (IsVerboseEnabled) LogVerbose($"Filter {filter} is not currently managed by any policy, cannot unmanage");
                    }
                }
            }
            else
            {
                if (IsVerboseEnabled) LogVerbose($"Filter {filter} is not indexed, cannot unmanage");
            }
        }
        /// <summary>
        /// Determines if the given <see cref="ThingFilter"/> can be managed by any active policy on this map. This method checks if there are any active policies, and if the filter is indexed and associated with an active policy. Returns true if the filter can be managed, false otherwise.
        /// </summary>
        /// <param name="thingFilter">The <see cref="ThingFilter"/> to check.</param>
        /// <returns>True if the filter can be managed; otherwise, false.</returns>
        public bool CouldManage(ThingFilter thingFilter)
        {
            thingFilter = Guard.NotNull(thingFilter, nameof(thingFilter));

            if(_thingFilters.Count == 0 && _defFilters.Count == 0)
            {
                if (IsVerboseEnabled) LogVerbose($"No active policies for map {map}, cannot manage any filters");
                return false;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table == null)
            {
                if (IsVerboseEnabled) LogVerbose($"ThingFilter table is not available for map {map}, cannot manage filter {thingFilter}");
                return false;
            }

            if (!table.TryFind<ThingFilter>(thingFilter, out var indexed))
            {
                if (IsVerboseEnabled) LogVerbose($"Filter {thingFilter} is not indexed, cannot be managed by any policy");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all policy names that currently have at least one active dynamic filter on this map that can filter defs.
        /// </summary>
        /// <returns>The active policy names.</returns>
        public IReadOnlyCollection<string> GetActiveDefPolicyNames()
        {
            lock (_lock)
            {
                return _defFilters.Keys;
            }
        }
        /// <summary>
        /// Gets all policy names that currently have at least one active dynamic filter on this map that can filter things.
        /// </summary>
        /// <returns>The active policy names.</returns>
        public IReadOnlyCollection<string> GetActiveThingPolicyNames()
        {
            lock (_lock)
            {
                return _thingFilters.Keys;
            }
        }

        /// <summary>
        /// Tries to get the policy currently managing <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The filter to resolve.</param>
        /// <param name="policyName">The managing policy name when found.</param>
        /// <returns>True when a managing policy exists; otherwise false.</returns>
        public bool TryGetManagedPolicyName(ThingFilter filter, bool isForThing, out string policyName)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            policyName = null;

            if (isForThing)
            {
                if (_filterToThingCache.TryGetValue(filter, out var cachedPolicyName))
                {
                    policyName = cachedPolicyName;
                    return true;
                }
            }
            else
            {
                if (_filterToDefCache.TryGetValue(filter, out var cachedPolicyName))
                {
                    policyName = cachedPolicyName;
                    return true;
                }
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                if (!string.IsNullOrWhiteSpace(storageId))
                {
                    if (isForThing)
                    {
                        if (_storageToThingFilterMap.TryGetValue(storageId, out var mappedPolicyName))
                        {
                            lock (_lock)
                            {
                                _filterToThingCache[filter] = mappedPolicyName;
                            }
                            policyName = mappedPolicyName;
                            return true;
                        }
                    }
                    else
                    {
                        if(_storageToDefFilterMap.TryGetValue(storageId, out var mappedPolicyName))
                        {
                            lock (_lock)
                            {
                                _filterToDefCache[filter] = mappedPolicyName;
                            }
                            policyName = mappedPolicyName;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all active <see cref="IDynamicFilter{Map, Thing}"/> instances that are currently active on this map. This method returns a list of filters that are associated with active policies, which can be used by other components or systems to apply the filtering logic defined by those policies.
        /// </summary>
        /// <returns>A list of active <see cref="IDynamicFilter{Map, Thing}"/> instances.</returns>
        public IEnumerable<IDynamicFilter<Map, Thing>> GetActiveThingFilters()
        {
            lock (_lock)
            {
                return _thingFilters.Values.ToList();
            }
        }
        /// <summary>
        /// Gets all active <see cref="IDynamicFilter{Map, ThingDef}"/> instances that are currently active on this map. This method returns a list of filters that are associated with active policies, which can be used by other components or systems to apply the filtering logic defined by those policies.
        /// </summary>
        /// <returns>A list of active <see cref="IDynamicFilter{Map, ThingDef}"/> instances.</returns>
        public IEnumerable<IDynamicFilter<Map, ThingDef>> GetActiveDefFilters()
        {
            lock (_lock)
            {
                return _defFilters.Values.ToList();
            }
        }

        /// <summary>
        /// Tries to get the active <see cref="IDynamicFilter{Map, ThingDef}"/> associated with the given policy name. Returns true if a filter is found, false otherwise.
        /// </summary>
        /// <param name="policyName">The name of the policy to look up.</param>
        /// <param name="filter">The filter associated with the policy, if found.</param>
        /// <returns>True if a filter is found; otherwise, false.</returns>
        public bool TryGetDefFilter(string policyName, out IDynamicFilter<Map, ThingDef> filter)
        {
            lock (_lock)
            {
                return _defFilters.TryGetValue(policyName, out filter);
            }
        }
        /// <summary>
        /// Tries to get the active <see cref="IDynamicFilter{Map, Thing}"/> associated with the given policy name. Returns true if a filter is found, false otherwise.
        /// </summary>
        /// <param name="policyName">The name of the policy to look up.</param>
        /// <param name="filter">The filter associated with the policy, if found.</param>
        /// <returns>True if a filter is found; otherwise, false.</returns>
        public bool TryGetThingFilter(string policyName, out IDynamicFilter<Map, Thing> filter)
        {
            lock (_lock)
            {
                return _thingFilters.TryGetValue(policyName, out filter);
            }
        }
        /// <summary>
        /// Tries to get the active <see cref="IDynamicFilter{Map, ThingDef}"/> associated with the given <see cref="ThingFilter"/>. This method looks up the storage ID of the filter and checks if there is an active policy managing it. Returns true if a filter is found, false otherwise.
        /// </summary>
        /// <param name="filter">The <see cref="ThingFilter"/> to look up.</param>
        /// <param name="activeFilter">The active filter associated with the <see cref="ThingFilter"/>, if found.</param>
        /// <returns>True if a filter is found; otherwise, false.</returns>
        public bool TryGetActiveDefFilter(ThingFilter filter, out IDynamicFilter<Map, ThingDef> activeFilter)
        {
            activeFilter = null;
            if(_filterCache.TryGetValue(filter, out var cachedFilter))
            {
                activeFilter = cachedFilter.Def;
                return activeFilter != null;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                if (!string.IsNullOrWhiteSpace(storageId) && _storageToDefFilterMap.TryGetValue(storageId, out var policyName))
                {
                    lock (_lock)
                    {
                        _filterToDefCache[filter] = policyName;
                    }
                    return TryGetDefFilter(policyName, out activeFilter);
                }
            }
            return false;
        }
        /// <summary>
        /// Tries to get the active <see cref="IDynamicFilter{Map, Thing}"/> associated with the given <see cref="ThingFilter"/>. This method looks up the storage ID of the filter and checks if there is an active policy managing it. Returns true if a filter is found, false otherwise.
        /// </summary>
        /// <param name="filter">The <see cref="ThingFilter"/> to look up.</param>
        /// <param name="activeFilter">The active filter associated with the <see cref="ThingFilter"/>, if found.</param>
        /// <returns>True if a filter is found; otherwise, false.</returns>
        public bool TryGetActiveThingFilter(ThingFilter filter, out IDynamicFilter<Map, Thing> activeFilter)
        {
            activeFilter = null;
            if(_filterCache.TryGetValue(filter, out var cachedFilter))
            {
                activeFilter = cachedFilter.Thing;
                return activeFilter != null;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                if (!string.IsNullOrWhiteSpace(storageId) && _storageToThingFilterMap.TryGetValue(storageId, out var policyName))
                {
                    lock (_lock)
                    {
                        _filterToThingCache[filter] = policyName;
                    }
                    return TryGetThingFilter(policyName, out activeFilter);
                }
            }
            return false;
        }

        /// <summary>
        /// Combined lookup for both thing and def filters in a single pass, avoiding duplicate index lookups.
        /// Returns true if either filter was found, false otherwise.
        /// </summary>
        internal bool TryGetActiveFilters(ThingFilter filter, out IDynamicFilter<Map, Thing> thingFilter, out bool thingFilterInverted, out IDynamicFilter<Map, ThingDef> defFilter, out bool defFilterInverted)
        {
            thingFilter = null;
            defFilter = null;
            thingFilterInverted = false;
            defFilterInverted = false;

            if (_filterCache.TryGetValue(filter, out var cachedfilters))
            {
                thingFilter = cachedfilters.Thing;
                defFilter = cachedfilters.Def;
                thingFilterInverted = cachedfilters.ThingInverted;
                defFilterInverted = cachedfilters.DefInverted;
                return thingFilter != null || defFilter != null;
            }
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
                var hasActiveDefFilter = TryGetActiveDefFilter(filter, out defFilter);
                defFilterInverted = hasActiveDefFilter ? _storageToInvertedDefFilterMap.ContainsKey(storageId) : false;
                var hasActiveThingFilter = TryGetActiveThingFilter(filter, out thingFilter);
                thingFilterInverted = hasActiveThingFilter ? _storageToInvertedThingFilterMap.ContainsKey(storageId) : false;

                _filterCache[filter] = (thingFilter, thingFilterInverted, defFilter, defFilterInverted);

                return hasActiveDefFilter || hasActiveThingFilter;
            }
            else
            {
                return false;
            }
        }

        private void MaintainActivePolicies(bool force = false)
        {
            ThingFilter[] activeFilters;
            lock (_lock)
            {
                activeFilters = _filterToDefCache.Keys.ToArray();
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if(table == null)
            {
                LogWarning($"ThingFilter table is not available while maintaining active policies for map {map}, skipping maintenance");
                return;
            }
            if (IsVerboseEnabled) LogVerbose($"Maintaining active policies for map {map}, currently managing {activeFilters.Length} filters");
            List<ThingDef> allDefs = null;
            foreach (var filter in activeFilters)
            {
                if(!table.TryFind<ThingFilter>(filter, out var indexed))
                {
                    lock (_lock)
                    {
                        Log($"Filter {filter} is no longer indexed, removing from cache and policy management for map {map}");
                        _filterToDefCache.Remove(filter);
                        _filterCache.Remove(filter);
                    }
                    continue;
                }
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);

                if (_filterToDefCache.TryGetValue(filter, out var policyName) && _defFilters.TryGetValue(policyName, out var defFilter))
                {
                    var wasUpdated = Invoking.Safe(() => defFilter.Update(StateStore.GetChildStore(map)), false);

                    if (wasUpdated || force)
                    {
                        var inverted = _storageToInvertedDefFilterMap.ContainsKey(storageId);
                        allDefs ??= DefDatabase<ThingDef>.AllDefsListForReading;
                        if (IsVerboseEnabled) LogVerbose($"Updating def allow list of size {allDefs.Count} using policy {policyName} on map {map} for filter {filter}");
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        for (int i = 0; i < allDefs.Count; i++)
                        {
                            var def = allDefs[i];
                            var isAllowed = defFilter.Filter(def);
                            if (inverted)
                            {
                                isAllowed = !isAllowed;
                            }
                            Invoking.Safe(() => filter.SetAllow(def, isAllowed));
                        }
                        stopwatch.Stop();
                        if (IsPerformanceEnabled) LogPerformance($"Finished updating def allow list of size {allDefs.Count} for filter {filter} using policy {policyName} in {stopwatch.Elapsed.TotalMilliseconds}ms");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void MapGenerated()
        {
            base.MapGenerated();
        }
        /// <inheritdoc/>
        public override void MapRemoved()
        {
            base.MapRemoved();
            _instances.Remove(map);

            var allActivePolicies = new HashSet<string>(_thingFilters.Keys.Concat(_defFilters.Keys));
            if (allActivePolicies.Count > 0)
            {
                if (IsVerboseEnabled) LogVerbose($"Deactivating policies {string.Join(", ", allActivePolicies)} for map {map}");
                foreach (var policyName in allActivePolicies)
                {
                    DeactivatePolicy(policyName);
                }
                if (IsVerboseEnabled) LogVerbose($"Finished deactivating {allActivePolicies.Count} policies for map {map}");
            }
            var hookManager = Toolkit.Hooks.Manager;
            hookManager.UnregisterAllBy<OnDynamicPolicyActivated>(this);
            hookManager.UnregisterAllBy<OnDynamicPolicyDeactivated>(this);
        }
        /// <inheritdoc/>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            var activePolicies = DynamicFiltersToolkit.Policies.ActivePolicies;
            if (activePolicies.Count > 0)
            {
                if (IsVerboseEnabled) LogVerbose($"Activating policies {string.Join(", ", activePolicies)} for map {map}");
                foreach (var policyName in activePolicies)
                {
                    ActivatePolicy(policyName);
                }
                if (IsVerboseEnabled) LogVerbose($"Finished activating {activePolicies.Count} policies for map {map}");
            }

            var hookManager = Toolkit.Hooks.Manager;
            hookManager.RegisterHook<OnDynamicPolicyActivated>(this);
            hookManager.RegisterHook<OnDynamicPolicyDeactivated>(this);
        }

        /// <inheritdoc/>
        bool IHook<OnDynamicPolicyActivated>.OnTrigger(OnDynamicPolicyActivated arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            ActivatePolicy(arg.Name);
            return true;
        }
        /// <inheritdoc/>
        bool IHook<OnDynamicPolicyDeactivated>.OnTrigger(OnDynamicPolicyDeactivated arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            DeactivatePolicy(arg.Name);
            return true;
        }

        /// <inheritdoc/>
        bool IHook<OnGameTickTrigger>.OnTrigger(OnGameTickTrigger arg)
        {
            var useLongTick = Toolkit.Settings.SlowGatheringEnabled;
            var tickerType = useLongTick ? TickerType.Long : TickerType.Rare;
            if (arg.TickerType == tickerType)
            {
                lock (_lock)
                {
                    foreach (var filter in _thingFilters.Values)
                    {
                        Invoking.Safe(() => filter.Update(StateStore.GetChildStore(map)));
                    }
                }
                MaintainActivePolicies();
                return true;
            }
            return false;
        }

        private void ActivatePolicy(string policyName)
        {
            policyName = Guard.NotNullOrWhitespace(policyName, nameof(policyName));
            _filterCache.Clear();
            lock (_lock)
            {
                var thingPolicy = Toolkit.Services.Get<IDynamicPolicy<Map, Thing>>(policyName);
                if (thingPolicy != null)
                {
                    Invoking.Safe(() =>
                    {
                        var filter = thingPolicy.GetFilter(map);
                        _thingFilters[policyName] = filter;
                        Log($"Activated policy filter {policyName} for map {map}");
                    });
                }
                var defPolicy = Toolkit.Services.Get<IDynamicPolicy<Map, ThingDef>>(policyName);
                if (defPolicy != null)
                {
                    Invoking.Safe(() =>
                    {
                        var filter = defPolicy.GetFilter(map);
                        _defFilters[policyName] = filter;
                        Log($"Activated policy filter {policyName} for map {map}");
                    });
                }
                MaintainActivePolicies(true);
            }
        }

        private void DeactivatePolicy(string policyName)
        {
            policyName = Guard.NotNullOrWhitespace(policyName, nameof(policyName));
            _filterCache.Clear();
            lock (_lock)
            {
                if (_thingFilters.TryGetValue(policyName, out var filter))
                {
                    _thingFilters.Remove(policyName);
                    if (filter is IDisposable disposable)
                    {
                        Invoking.Safe(() => disposable.Dispose());
                    }
                    Log($"Deactivated policy filter {policyName} for map {map}");
                }
                if (_defFilters.TryGetValue(policyName, out var defFilter))
                {
                    _defFilters.Remove(policyName);
                    if (defFilter is IDisposable disposable)
                    {
                        Invoking.Safe(() => disposable.Dispose());
                    }
                    Log($"Deactivated policy filter {policyName} for map {map}");
                }
            }
        }
        
        /// <inheritdoc/>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _storageToDefFilterMap, "filterToPolicyMap", LookMode.Value, LookMode.Value);
            _storageToDefFilterMap ??= new Dictionary<string, string>();
            Scribe_Collections.Look(ref _storageToThingFilterMap, "filterToThingPolicyMap", LookMode.Value, LookMode.Value);
            _storageToThingFilterMap ??= new Dictionary<string, string>();
            Scribe_Collections.Look(ref _storageToInvertedDefFilterMap, "filterToInvertedPolicyMap", LookMode.Value, LookMode.Value);
            _storageToInvertedDefFilterMap ??= new Dictionary<string, string>();
            Scribe_Collections.Look(ref _storageToInvertedThingFilterMap, "filterToInvertedThingPolicyMap", LookMode.Value, LookMode.Value);
            _storageToInvertedThingFilterMap ??= new Dictionary<string, string>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _filterToDefCache.Clear();
                _filterToThingCache.Clear();
                _filterCache.Clear();
                if (IsVerboseEnabled) LogVerbose($"Post-load initialization of MapPolicyManager for map {map}, clearing filter cache and maintaining {_storageToDefFilterMap.Count}/{_storageToThingFilterMap.Count} active def/thing policies");
                MaintainActivePolicies(true);
            }
        }
    }
}
