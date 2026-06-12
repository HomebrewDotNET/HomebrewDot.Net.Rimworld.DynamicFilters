using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Filtering.Triggers
{
    /// <summary>
    /// Raied when a dynamic policy is activated.
    /// </summary>
    public class OnDynamicPolicyActivated
    {
        /// <summary>
        /// The name of the activated policy. This can be used by listeners to determine if they need to update their filters based on the activated policy.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc cref="OnDynamicPolicyActivated"/>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        public OnDynamicPolicyActivated(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// Raied when a dynamic policy is deactivated.
    /// </summary>
    public class OnDynamicPolicyDeactivated
    {
        /// <summary>
        /// The name of the deactivated policy. This can be used by listeners to determine if they need to update their filters based on the deactivated policy.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc cref="OnDynamicPolicyDeactivated"/>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        public OnDynamicPolicyDeactivated(string name)
        {
            Name = name;
        }
    }
}
