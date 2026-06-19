using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.State;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Filtering.Components
{
    /// <summary>
    /// A dynamic filter that uses delegates to determine if an item matches and to update its state.
    /// </summary>
    /// <typeparam name="TScope">The type of the scope.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class DelegateDynamicFilter<TScope, TItem> : IDynamicFilter<TScope, TItem>
    {
        // Fields
        private readonly Func<TScope, TItem, bool> _filterFunc;
        private readonly Func<TScope, IStateStore<TScope>, bool> _updateFunc;

        // Properties
        /// <inheritdoc/>
        public TScope Scope { get; }
        /// <inheritdoc/>
        public IDynamicPolicy<TScope, TItem> Policy { get; }

        /// <inheritdoc cref="DelegateDynamicFilter{TScope, TItem}"/>
        /// <param name="scope">The scope of the filter.</param>
        /// <param name="policy">The policy of the filter.</param>
        /// <param name="filter">The filter function.</param>
        /// <param name="update">The update function.</param>
        public DelegateDynamicFilter(TScope scope, IDynamicPolicy<TScope, TItem> policy, Func<TScope, TItem, bool> filter, Func<TScope, IStateStore<TScope>, bool> update = null)
        {
            Scope = Guard.NotNull(scope, nameof(scope));
            Policy = Guard.NotNull(policy, nameof(policy));
            _filterFunc = Guard.NotNull(filter, nameof(filter));
            _updateFunc = update;
        }

        /// <inheritdoc/>
        public bool Filter(TItem item)
        {
            return _filterFunc(Scope, item);
        }
        /// <inheritdoc/>
        public bool Update(IStateStore<TScope> stateStore)
        {
            return _updateFunc?.Invoke(Scope, stateStore) ?? false;
        }
    }
}
