using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.State;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Policies.Components
{
    /// <summary>
    /// Policy that manages filters based on collections of items.
    /// </summary>
    public class CollectionPolicy : IDynamicPolicy<Map, ThingDef>, IDynamicPolicy<Map, Thing>, IDisposable
    {
        // Fields
        private readonly string _name;

        // State
        internal int _filterTracker;

        // Properties
        /// <inheritdoc/>
        string IDynamicPolicy<Map, Thing>.Name => _name;
        /// <inheritdoc/>
        string IDynamicPolicy<Map, ThingDef>.Name => _name;

        /// <inheritdoc cref="CollectionPolicy"/>
        /// <param name="name">The name of the policy and the backing collection</param>
        public CollectionPolicy(string name)
        {
            _name = Guard.NotNullOrWhitespace(name, nameof(name));
        }
        /// <inheritdoc/>
        IDynamicFilter<Map, Thing> IDynamicPolicy<Map, Thing>.GetFilter(Map scope)
        {
            scope = Guard.NotNull(scope, nameof(scope));

            var mapCollectionName = $"{scope.GetUniqueLoadID()}.{_name}";
            Toolkit.Collecting.Build(mapCollectionName, x => x.Compare.Indexed(nameof(Map))
                                                              .With.Equal(scope)
                                                              .CollectFromCollection<ICollectionBuilder, Thing>(_name)
                                    );
            return new Filter<Thing>(mapCollectionName, scope, this);
        }
        /// <inheritdoc/>
        IDynamicFilter<Map, ThingDef> IDynamicPolicy<Map, ThingDef>.GetFilter(Map scope)
        {
            scope = Guard.NotNull(scope, nameof(scope));

            // Defs are not really scoped per map so no need for extra filtering
            _filterTracker++;
            return new Filter<ThingDef>(_name, scope, this);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_filterTracker <= 0)
                Toolkit.Collecting.Remove(_name);
        }


        private class Filter<T> : IDynamicFilter<Map, T>, IDisposable where T : class
        {
            // Fields
            private readonly string _collectionName;
            private readonly CollectionPolicy _policy;

            // State
            private ICollector<T> _collection;
            private int _lastCollectionVersion = -1;

            // Properties
            /// <inheritdoc/>
            public Map Scope { get; }
            /// <inheritdoc/>
            public IDynamicPolicy<Map, T> Policy => (IDynamicPolicy<Map, T>)_policy;

            public Filter(string collectionName, Map scope, CollectionPolicy policy)
            {
                _collectionName = Guard.NotNullOrWhitespace(collectionName, nameof(collectionName));
                Scope = Guard.NotNull(scope, nameof(scope));
                _policy = Guard.NotNull(policy, nameof(policy));
                if (Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    _collection = typedCollector;
                }
            }
            /// <inheritdoc/>
            public bool Update(IStateStore<Map> stateStore)
            {
                bool isNew = false;
                if (Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    isNew = _collection != typedCollector;
                    _collection = typedCollector;
                }
                if (_collection is SnapshotCollector<T> snapshotCollector)
                {
                    if (snapshotCollector.Version != _lastCollectionVersion)
                    {
                        _lastCollectionVersion = snapshotCollector.Version;
                        return true;
                    }
                }
                else
                {
                    // If it's not a snapshot collector we assume it's always updated since we don't have versioning for other collector types
                    return true;
                }
                return isNew;
            }
            /// <inheritdoc/>
            bool IDynamicFilter<Map, T>.Filter(T item)
            {
                var collection = _collection;
                if (collection is not null)
                {
                    return collection.Contains(item);
                }
                return false;
            }
            /// <inheritdoc/>
            public void Dispose()
            {
                if (typeof(ThingDef) == typeof(T))
                {
                    _policy._filterTracker--;
                }
                else
                {
                    Toolkit.Collecting.Remove(_collectionName);
                }
            }
        }
    }
}
