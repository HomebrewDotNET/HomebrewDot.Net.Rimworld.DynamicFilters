using System;
using System.Collections.Generic;
using System.Linq;
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

        // Fields
        private readonly object _lock = new object();
        private readonly Dictionary<string, IDynamicFilter<Map, Thing>> _thingFilters = new Dictionary<string, IDynamicFilter<Map, Thing>>();
        private readonly Dictionary<string, IDynamicFilter<Map, ThingDef>> _defFilters = new Dictionary<string, IDynamicFilter<Map, ThingDef>>();
        private Dictionary<ThingFilter, string> _filterCache = new Dictionary<ThingFilter, string>();
        private Dictionary<string, string> _filterToPolicyMap = new Dictionary<string, string>();


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

        }
        /// <inheritdoc/>
        public bool ManageWith(ThingFilter filter, string policyName)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            policyName = Guard.NotNullOrWhitespace(policyName, nameof(policyName));
            if (!_thingFilters.ContainsKey(policyName) && !_defFilters.ContainsKey(policyName))
            {
                LogVerbose($"Policy {policyName} is not active for map {map}, cannot manage filter {filter} with this policy");
                return false;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                lock (_lock)
                {
                    var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey);
                    if (string.IsNullOrWhiteSpace(storageId))
                    {
                        LogWarning($"Filter {filter} is not properly indexed (missing storage ID), cannot manage with policy {policyName}");
                        return false;
                    }

                    if (_filterToPolicyMap.TryGetValue(storageId, out var existingPolicy))
                    {
                        if (existingPolicy == policyName)
                        {
                            LogVerbose($"Filter {filter} is already managed by policy {policyName}, skipping");
                            return true;
                        }
                        else
                        {
                            Log($"Filter {filter} is already managed by policy {existingPolicy}, unmanaging it before assigning to policy {policyName}");
                            Unmanage(filter);
                        }
                    }
                    _filterToPolicyMap[storageId] = policyName;
                    _filterCache[filter] = policyName;
                    Log($"Filter {filter} on {storageId} is now managed by policy {policyName}");
                    MaintainActivePolicies();
                    return true;
                }
            }
            else
            {
                LogVerbose($"Filter {filter} is not indexed, cannot manage with policy {policyName}");
                return false;
            }
        }
        public void Unmanage(ThingFilter filter)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            _filterCache.Remove(filter);
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                lock (_lock)
                {
                    var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey);
                    if (!string.IsNullOrWhiteSpace(storageId) && _filterToPolicyMap.TryGetValue(storageId, out var policyName))
                    {
                        _filterToPolicyMap.Remove(storageId);
                        Log($"Filter {filter} is no longer managed by policy {policyName}");
                    }
                    else
                    {
                        LogVerbose($"Filter {filter} is not currently managed by any policy, cannot unmanage");
                    }
                }
            }
            else
            {
                LogVerbose($"Filter {filter} is not indexed, cannot unmanage");
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
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();

            if(_thingFilters.Count == 0 && _defFilters.Count == 0)
            {
                LogVerbose($"No active policies for map {map}, cannot manage any filters");
                return false;
            }

            if (!table.TryFind<ThingFilter>(thingFilter, out var indexed))
            {
                LogVerbose($"Filter {thingFilter} is not indexed, cannot be managed by any policy");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all policy names that currently have at least one active dynamic filter on this map.
        /// </summary>
        /// <returns>The active policy names.</returns>
        public IReadOnlyCollection<string> GetActivePolicyNames()
        {
            lock (_lock)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in _thingFilters.Keys)
                {
                    names.Add(key);
                }
                foreach (var key in _defFilters.Keys)
                {
                    names.Add(key);
                }

                return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Tries to get the policy currently managing <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The filter to resolve.</param>
        /// <param name="policyName">The managing policy name when found.</param>
        /// <returns>True when a managing policy exists; otherwise false.</returns>
        public bool TryGetManagedPolicyName(ThingFilter filter, out string policyName)
        {
            filter = Guard.NotNull(filter, nameof(filter));
            policyName = null;

            if (_filterCache.TryGetValue(filter, out var cachedPolicyName))
            {
                policyName = cachedPolicyName;
                return true;
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey);
                if (!string.IsNullOrWhiteSpace(storageId) && _filterToPolicyMap.TryGetValue(storageId, out var mappedPolicyName))
                {
                    lock (_lock)
                    {
                        _filterCache[filter] = mappedPolicyName;
                    }
                    policyName = mappedPolicyName;
                    return true;
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
            if (_filterCache.TryGetValue(filter, out var cachedPolicyName))
            {
                return TryGetDefFilter(cachedPolicyName, out activeFilter);
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey);
                if (!string.IsNullOrWhiteSpace(storageId) && _filterToPolicyMap.TryGetValue(storageId, out var policyName))
                {
                    lock (_lock)
                    {
                        _filterCache[filter] = policyName;
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
            if (_filterCache.TryGetValue(filter, out var cachedPolicyName))
            {
                return TryGetThingFilter(cachedPolicyName, out activeFilter);
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if (table != null && table.TryFind<ThingFilter>(filter, out var indexed))
            {
                var storageId = indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey);
                if (!string.IsNullOrWhiteSpace(storageId) && _filterToPolicyMap.TryGetValue(storageId, out var policyName))
                {
                    lock (_lock)
                    {
                        _filterCache[filter] = policyName;
                    }
                    return TryGetThingFilter(policyName, out activeFilter);
                }
            }
            return false;
        }

        private void MaintainActivePolicies()
        {
            ThingFilter[] activeFilters;
            lock (_lock)
            {
                activeFilters = _filterCache.Keys.ToArray();
            }

            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if(table == null)
            {
                LogWarning($"ThingFilter table is not available while maintaining active policies for map {map}, skipping maintenance");
                return;
            }
            LogVerbose($"Maintaining active policies for map {map}, currently managing {activeFilters.Length} filters");
            foreach (var filter in activeFilters)
            {
                if(!table.TryFind<ThingFilter>(filter, out _))
                {
                    lock (_lock)
                    {
                        LogVerbose($"Filter {filter} is no longer indexed, removing from cache and policy management for map {map}");
                        _filterCache.Remove(filter);
                    }
                }

                if(_filterCache.TryGetValue(filter, out var policyName) && _defFilters.TryGetValue(policyName, out var defFilter))
                {
                    LogVerbose($"Updating def allow list using policy {policyName} on map {map} for filter {filter}");
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                    for(int i = 0; i < allDefs.Count; i++)
                    {
                        var def = allDefs[i];
                        Invoking.Safe(() => filter.SetAllow(def, defFilter.Filter(def)));
                    }
                    stopwatch.Stop();
                    LogVerbose($"Finished updating def allow list for filter {filter} using policy {policyName} in {stopwatch.ElapsedMilliseconds} ms");
                }
            }
        }

        /// <inheritdoc/>
        public override void MapGenerated()
        {
            base.MapGenerated();

            var activePolicies = DynamicFiltersToolkit.Policies.ActivePolicies;
            if (activePolicies.Count > 0)
            {
                LogVerbose($"Activating policies {string.Join(", ", activePolicies)} for map {map}");
                foreach (var policyName in activePolicies)
                {
                    ActivatePolicy(policyName);
                }
                LogVerbose($"Finished activating {activePolicies.Count} policies for map {map}");
            }

            var hookManager = Toolkit.Hooks.Manager;
            hookManager.RegisterHook<OnDynamicPolicyActivated>(this);
            hookManager.RegisterHook<OnDynamicPolicyDeactivated>(this);
        }
        /// <inheritdoc/>
        public override void MapRemoved()
        {
            base.MapRemoved();

            var allActivePolicies = new HashSet<string>(_thingFilters.Keys.Concat(_defFilters.Keys));
            if (allActivePolicies.Count > 0)
            {
                LogVerbose($"Deactivating policies {string.Join(", ", allActivePolicies)} for map {map}");
                foreach (var policyName in allActivePolicies)
                {
                    DeactivatePolicy(policyName);
                }
                LogVerbose($"Finished deactivating {allActivePolicies.Count} policies for map {map}");
            }
            var hookManager = Toolkit.Hooks.Manager;
            hookManager.UnregisterAllBy<OnDynamicPolicyActivated>(this);
            hookManager.UnregisterAllBy<OnDynamicPolicyDeactivated>(this);
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
                    foreach (var defFilter in _defFilters.Values)
                    {
                        Invoking.Safe(() => defFilter.Update(StateStore.GetChildStore(map)));
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
            lock (_lock)
            {
                var thingPolicy = Toolkit.Services.Get<IDynamicPolicy<Map, Thing>>(policyName);
                if (thingPolicy != null)
                {
                    Invoking.Safe(() =>
                    {
                        var filter = thingPolicy.GetFilter(map);
                        _thingFilters[policyName] = filter;
                        filter.Update(StateStore.GetChildStore(map));
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
                        filter.Update(StateStore.GetChildStore(map));
                        Log($"Activated policy filter {policyName} for map {map}");
                    });
                }
            }
        }

        private void DeactivatePolicy(string policyName)
        {
            policyName = Guard.NotNullOrWhitespace(policyName, nameof(policyName));
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

            Scribe_Collections.Look(ref _filterToPolicyMap, "filterToPolicyMap", LookMode.Value, LookMode.Value);
        }
    }
}
