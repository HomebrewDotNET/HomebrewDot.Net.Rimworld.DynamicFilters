using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.State
{
    /// <summary>
    /// Generic store for storing additional state on object.
    /// </summary>
    public interface IStateStore<out T> : IDictionary<string, object>
    {
        /// <summary>
        /// The index is stored on.
        /// Can be null if it's the root store.
        /// </summary>
        T Instance { get; }

        /// <summary>
        /// Get or create a child store for the given instance.
        /// </summary>
        /// <typeparam name="TChild">The type of the child instance.</typeparam>
        /// <param name="instance">The child instance.</param>
        /// <returns>The child state store for the given instance.</returns>
        IStateStore<TChild> GetChildStore<TChild>(TChild instance);
        /// <summary>
        /// Destroys the child store for the given instance.
        /// </summary>
        /// <typeparam name="TChild">The type of the child instance.</typeparam>
        /// <param name="instance">The child instance.</param>
        /// <returns>True if the child store was successfully destroyed; otherwise, false.</returns>
        bool DestroyChildStore<TChild>(TChild instance);
    }
}
