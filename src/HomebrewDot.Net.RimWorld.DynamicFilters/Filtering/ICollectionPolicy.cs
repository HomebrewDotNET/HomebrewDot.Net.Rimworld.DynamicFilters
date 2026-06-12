using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting;

namespace HomebrewDot.Net.Rimworld.Filtering
{
    /// <summary>
    /// A policy that is based on backing <see cref="ICollector{T}"/>(s).
    /// </summary>
    /// <typeparam name="TScope">The type of the scope.</typeparam>
    /// <typeparam name="TItem">The type of the items to be filtered.</typeparam>
    public interface ICollectionPolicy<TScope, TItem> : IDynamicPolicy<TScope, TItem> where TItem : class
    {
        /// <summary>
        /// The main collection that this filter is based on. This collection is used to determine which items are included in the filter.
        /// </summary>
        ICollector<TItem> Collection { get; }
        /// <summary>
        /// Optional fallback collections that will be used (in order) when the previous collection is empty.
        /// Allows you for example to filter on a 'Sniper' collection but fallback to a 'Ranged' then 'Melee' collection so filter always returns something based on the best available collection.
        /// </summary>
        IReadOnlyList<ICollector<TItem>> FallbackCollections { get; }
    }
}
