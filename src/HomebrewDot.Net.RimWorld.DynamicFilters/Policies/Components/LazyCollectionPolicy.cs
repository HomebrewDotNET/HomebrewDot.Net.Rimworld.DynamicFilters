using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.State;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Policies.Components
{
    /// <summary>
    /// Thing policy that lazily evaluates conditions without storing results in a collection. This is useful for filters that require lower latency or that change frequently.
    /// </summary>
    public class LazyCollectionPolicy : IDynamicPolicy<Map, Thing>, IDisposable
    {
        // Fields
        private readonly string _name;
        private readonly ICollectionDef _collection;
        private readonly ICollectionComparator _comparator;
        private readonly IReadOnlyDictionary<string, ICollectionDef> _collections;
        private readonly IDatabase<Thing> _database;

        // Properties
        /// <inheritdoc/>
        string IDynamicPolicy<Map, Thing>.Name => _name;

        /// <inheritdoc cref="CollectionPolicy"/>
        /// <param name="name">The name of the policy and the backing collection</param>
        /// <param name="collection">The collection definition to use for the policy</param>
        /// <param name="comparator">The comparator to use for the policy</param>
        /// <param name="collections">The collection definitions to use for the policy</param>
        /// <param name="database">The database to use for the policy</param>
        public LazyCollectionPolicy(string name, ICollectionDef collection, ICollectionComparator comparator, IReadOnlyDictionary<string, ICollectionDef> collections, IDatabase<Thing> database)
        {
            _name = Guard.NotNullOrWhitespace(name, nameof(name));
            _collection = Guard.NotNull(collection, nameof(collection));
            _comparator = Guard.NotNull(comparator, nameof(comparator));
            _collections = Guard.NotNull(collections, nameof(collections));
            _database = database;
        }
        /// <inheritdoc/>
        IDynamicFilter<Map, Thing> IDynamicPolicy<Map, Thing>.GetFilter(Map scope)
        {
            scope = Guard.NotNull(scope, nameof(scope));

            return new Filter<Thing>(_collection, _comparator, _collections, scope, this, _database);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Toolkit.Collecting.Remove(_name);
        }

        private class Filter<T> : IDynamicFilter<Map, T> where T : class
        {
            // Fields
            private readonly LazyCollectionPolicy _policy;
            private readonly ICollectionDef _collection;
            private readonly ICollectionComparator _comparator;
            private readonly IReadOnlyDictionary<string, ICollectionDef> _collections;
            private readonly IReadOnlyDictionary<string, object> _context;
            private readonly IDatabase<Thing> _database;

            // Properties
            /// <inheritdoc/>
            public Map Scope { get; }
            /// <inheritdoc/>
            public IDynamicPolicy<Map, T> Policy => (IDynamicPolicy<Map, T>)_policy;

            public Filter(ICollectionDef collection, ICollectionComparator comparator, IReadOnlyDictionary<string, ICollectionDef> collections, Map scope, LazyCollectionPolicy policy, IDatabase<Thing> database)
            {
                Scope = Guard.NotNull(scope, nameof(scope));
                _policy = Guard.NotNull(policy, nameof(policy));
                _collection = Guard.NotNull(collection, nameof(collection));
                _comparator = Guard.NotNull(comparator, nameof(comparator));
                _database = database;
                _collections = Guard.NotNull(collections, nameof(collections));
                _context = new Dictionary<string, object>
                {
                    { nameof(Map), scope }
                };
            }
            /// <inheritdoc/>
            public bool Update(IStateStore<Map> stateStore)
            {
                return false;
            }
            /// <inheritdoc/>
            bool IDynamicFilter<Map, T>.Filter(T item)
            {
                if(_database is not null && item is Thing thing)
                {
                    var indexed = _database.Find(thing);
                    if(indexed is not null)
                    {
                        return _comparator.Matches(_collection, indexed, _collections, _context);
                    }
                }
                return _comparator.Matches(_collection, item, _collections, _context);
            }
        }
    }
}
