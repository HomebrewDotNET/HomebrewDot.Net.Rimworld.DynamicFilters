using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.Rimworld.Filtering
{
    /// <summary>
    /// A policy that can generate filters based on a scope.
    /// </summary>
    /// <typeparam name="TScope">The type of the scope.</typeparam>
    /// <typeparam name="TItem">The type of the items to be filtered.</typeparam>
    public interface IDynamicPolicy<TScope, in TItem> where TScope : class
    {
        /// <summary>
        /// The unique name of this policy.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a filter for the given scope.
        /// </summary>
        /// <param name="scope">The scope for which to get the filter.</param>
        /// <returns>A dynamic filter for the given scope.</returns>
        public IDynamicFilter<TScope, TItem> GetFilter(TScope scope);
    }
}
