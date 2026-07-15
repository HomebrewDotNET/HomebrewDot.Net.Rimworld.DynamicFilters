using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Filtering
{
    /// <summary>
    /// Responsible for providing dynamic policies and its dependencies.
    /// </summary>
    public interface IDynamicPolicyProvider
    {
        /// <summary>
        /// Activates the provider, allowing it to register dynamic policies that will be used for filtering. This method is called by the framework when the provider is initialized, and it should use the provided context to register any dynamic policies that it wants to make available for filtering and it's dependencies.
        /// </summary>
        /// <param name="name">The unique name for the policies activated.</param>
        /// <param name="context">The context for activating the provider.</param>
        void Activate(string name, IDynamicPolicyProviderActivationContext context);
        /// <summary>
        /// Deactivates the provider, allowing it to clean up any resources or unregister dynamic policies that it previously registered. This method is called by the framework when the provider is being deactivated.
        /// </summary>
        /// <param name="disposePolicies">An action that the provider can call to dispose of any policies it has registered. When not called it will be called after this method.</param>
        void Deactivate(Action disposePolicies);
    }

    /// <summary>
    /// Context provided to <see cref="IDynamicPolicyProvider"/> when it is activated. This context allows the provider to register dynamic policies that will be used for filtering.
    /// </summary>
    public interface IDynamicPolicyProviderActivationContext
    {
        /// <summary>
        /// Registers a dynamic policy to be available for filtering. The policy will be used to create filters for the specified scope and item types.
        /// </summary>
        /// <typeparam name="TScope">The type of the scope for which the policy is applicable.</typeparam>
        /// <typeparam name="TItem">The type of the item that the policy will filter.</typeparam>
        /// <param name="policy">The dynamic policy to be registered.</param>
        ///<returns>The activation context, allowing for fluent chaining</returns>
        IDynamicPolicyProviderActivationContext AvailableFor<TScope, TItem>(IDynamicPolicy<TScope, TItem> policy) where TScope : class;
        /// <summary>
        /// Small name for the label of the activated policies. Used in the UI to display to the player.
        /// If not provided the type name of the provider will be used, but it is recommended to provide a custom label for better user experience.
        /// </summary>
        /// <param name="label">The label to use</param>
        /// <returns>The activation context, allowing for fluent chaining</returns>
        IDynamicPolicyProviderActivationContext WithLabel(string label);
        /// <summary>
        /// Full name for the label of the activated policies. Used in the UI to display to the player. If not provided, it will default to the same value as <see cref="WithLabel(string)"/>.
        /// </summary>
        /// <param name="title">The title to use</param>
        /// <returns>The activation context, allowing for fluent chaining</returns>
        IDynamicPolicyProviderActivationContext WithTitle(string title);
        /// <summary>
        /// Description for the activated policies. Used in the UI to display to the player. If not provided, it will default to an empty string.
        /// </summary>
        /// <param name="description">The description to use</param>
        /// <returns>The activation context, allowing for fluent chaining</returns>
        IDynamicPolicyProviderActivationContext WithDescription(string description);
    }
}
