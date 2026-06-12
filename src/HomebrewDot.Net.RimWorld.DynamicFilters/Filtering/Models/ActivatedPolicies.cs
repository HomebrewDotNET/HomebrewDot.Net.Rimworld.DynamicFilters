using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Filtering.Models
{
    /// <summary>
    /// Represents a dynamic policy that has been activated by the player. This is used to show the player which policies are currently active and to provide information about them in the UI.
    /// </summary>
    public class ActivatedPolicies : IDynamicPolicyProviderActivationContext, IDisposable
    {
        // Fields
        private readonly List<object> _policies = new List<object>();
        private Action _disposeActions = () => { };

        // Properties
        /// <summary>
        /// The unique name of the activated policies. This can be used by listeners to determine if they need to update their filters based on the activated policies.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Small name for the label of the activated policies. Used in the UI to display to the player.
        /// </summary>
        public string Label { get; private set; }
        /// <summary>
        /// Full name for the label of the activated policies. Used in the UI to display to the player.
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// Description for the activated policies. Used in the UI to display to the player.
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// The provider that activated these policies.
        /// </summary>
        public IDynamicPolicyProvider Provider { get; }

        /// <inheritdoc cref="ActivatedPolicies"/>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="dynamicPolicyProvider"><inheritdoc cref="Provider"/></param>
        public ActivatedPolicies(string name, IDynamicPolicyProvider dynamicPolicyProvider)
        {
            Name = Guard.NotNullOrWhitespace(name, nameof(name));
            Provider = Guard.NotNull(dynamicPolicyProvider, nameof(dynamicPolicyProvider));
            Label = dynamicPolicyProvider.GetType().Name;
            Title = Label;
            Description = string.Empty;
        }
        /// <inheritdoc/>
        IDynamicPolicyProviderActivationContext IDynamicPolicyProviderActivationContext.AvailableFor<TScope, TItem>(IDynamicPolicy<TScope, TItem> policy)
        {
            policy = Guard.NotNull(policy, nameof(policy));
            Toolkit.Services.Register<IDynamicPolicy<TScope, TItem>>(policy, Name);
            _policies.Add(policy);
            _disposeActions += () => Invoking.Safe(() => Toolkit.Services.UnregisterByName<IDynamicPolicy<TScope, TItem>>(Name));
            return this;
        }
        /// <inheritdoc/>
        IDynamicPolicyProviderActivationContext IDynamicPolicyProviderActivationContext.WithDescription(string description)
        {
            Description = Guard.NotNull(description, nameof(description));
            return this;
        }
        /// <inheritdoc/>
        IDynamicPolicyProviderActivationContext IDynamicPolicyProviderActivationContext.WithLabel(string label)
        {
            Label = Guard.NotNullOrWhitespace(label, nameof(label));
            return this;
        }
        /// <inheritdoc/>
        IDynamicPolicyProviderActivationContext IDynamicPolicyProviderActivationContext.WithTitle(string title)
        {
            Title = Guard.NotNullOrWhitespace(title, nameof(title));
            return this;
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Invoking.Safe(() => _disposeActions());
        }
    }
}
