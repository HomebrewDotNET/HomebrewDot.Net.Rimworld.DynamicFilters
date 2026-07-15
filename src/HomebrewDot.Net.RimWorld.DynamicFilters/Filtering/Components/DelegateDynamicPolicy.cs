using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Filtering.Components
{
    /// <summary>
    /// A dynamic policy that uses delegates to generate filters.
    /// </summary>
    /// <typeparam name="TScope">The type of the scope for the filter.</typeparam>
    /// <typeparam name="TItem">The type of the items to be filtered.</typeparam>
    public class DelegateDynamicPolicy<TScope, TItem> : IDynamicPolicy<TScope, TItem> where TScope : class
    {
        // Fields
        private readonly Func<TScope, IDynamicFilter<TScope, TItem>> _filterFactory;
        // Properties
        ///<inheritdoc/>
        public string Name { get; }

        /// <inheritdoc cref="DelegateDynamicPolicy{TScope, TItem}"/>
        /// <param name="name">The name of the dynamic policy.</param>
        /// <param name="filterFactory">A delegate that generates the filter based on the scope.</param>
        public DelegateDynamicPolicy(string name, Func<TScope, IDynamicFilter<TScope, TItem>> filterFactory)
        {
            Name = Guard.NotNullOrWhitespace(name, nameof(name));
            _filterFactory = Guard.NotNull(filterFactory, nameof(filterFactory));
        }

        ///<inheritdoc/>
        public IDynamicFilter<TScope, TItem> GetFilter(TScope scope)
        {
            return _filterFactory(scope);
        }
    }
}
