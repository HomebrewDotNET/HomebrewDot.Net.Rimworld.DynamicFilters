using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.State.Components
{
    /// <summary>
    /// Default implementation of <see cref="IStateStore{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the state store.</typeparam>
    public class StateStore<T>: IStateStore<T>
    {
        // Statics
        /// <summary>
        /// The root state store. This store is not associated with any instance and can be used to store global state.
        /// </summary>
        public static StateStore<object> Root { get; } = new();

        // Fields
        private readonly Dictionary<string, object> _state = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, object> _childStores = new();
        private readonly object _lock = new();

        // Properties
        /// <inheritdoc/>
        public T Instance { get; }
        /// <inheritdoc/>
        ICollection<string> IDictionary<string, object>.Keys => _state.Keys;
        /// <inheritdoc/>
        ICollection<object> IDictionary<string, object>.Values => _state.Values;
        /// <inheritdoc/>
        int ICollection<KeyValuePair<string, object>>.Count => _state.Count;
        /// <inheritdoc/>
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;
        /// <inheritdoc/>
        public object this[string key] { get => _state[key]; set => _state[key] = value; }

        /// <inheritdoc cref="StateStore{T}"/>
        /// <param name="instance"><inheritdoc cref="Instance"/></param>
        public StateStore(T instance)
        {
            Instance = Guard.NotNull(instance, nameof(instance));
        }

        private StateStore()
        {
            
        }

        /// <inheritdoc/>
        bool IStateStore<T>.DestroyChildStore<TChild>(TChild instance)
        {
            lock(_lock)
            {
                return _childStores.Remove(instance);
            }
        }
        /// <inheritdoc/>
        IStateStore<TChild> IStateStore<T>.GetChildStore<TChild>(TChild instance)
        {
            if(!_childStores.TryGetValue(instance, out var store))
            {
                lock (_lock)
                {
                    if (!_childStores.TryGetValue(instance, out store))
                    {
                        store = new StateStore<TChild>(instance);
                        _childStores[instance] = store;
                    }
                    return (IStateStore<TChild>)store;
                }
            }
            return (IStateStore<TChild>)store;
        }

        /// <inheritdoc/>
        void IDictionary<string, object>.Add(string key, object value) => _state.Add(key, value);
        /// <inheritdoc/>
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => _state.Add(item.Key, item.Value);
        /// <inheritdoc/>
        void ICollection<KeyValuePair<string, object>>.Clear() => _state.Clear();
        /// <inheritdoc/>
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _state.Contains(item);
        /// <inheritdoc/>
        bool IDictionary<string, object>.ContainsKey(string key) => _state.ContainsKey(key);
        /// <inheritdoc/>
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object>>)_state).CopyTo(array, arrayIndex);
        /// <inheritdoc/>
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _state.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _state.GetEnumerator();
        /// <inheritdoc/>
        bool IDictionary<string, object>.Remove(string key) => _state.Remove(key);
        /// <inheritdoc/>
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => _state.Remove(item.Key);
        /// <inheritdoc/>
        bool IDictionary<string, object>.TryGetValue(string key, out object value) => _state.TryGetValue(key, out value);
    }
}
