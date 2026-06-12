using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Filtering;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.Configuration
{
    /// <summary>
    /// Represents a template for a <see cref="IDynamicPolicyProvider"/> that can be configured by the user by providing input.
    /// </summary>
    public interface IDynamicPolicyTemplate
    {
        /// <summary>
        /// The unique key for this template. This is used to identify the template and should be unique across all templates.
        /// This will be saved in the configuration file to identify which template to use for a given policy provider. It should be unique across all templates to avoid conflicts. It is recommended to use a namespaced key, for example 'MyMod.RangedWeaponFilter' to avoid conflicts with other mods.
        /// </summary>
        string StorageKey { get; }
        /// <summary>
        /// Returns the title (header) of the template for the UI.
        /// </summary>
        /// <returns>The title for the UI.</returns>
        string GetTitle();
        /// <summary>
        /// Returns a short description of the template. Shown in the UI under the title.
        /// </summary>
        /// <returns>A short description of the template.</returns>
        string GetShortDescription();
        /// <summary>
        /// Returns a long description of the policies created by this template. This can be used to provide more detailed information about the policies created by this template and how they work. Shown in the UI when the template is selected.
        /// For example, if the template is for ranged weapons, the long description could be 'Creates a filter that includes all ranged weapons with a range above n'
        /// </summary>
        /// <param name="settings">The settings object created by <see cref="DrawSettings"/>. This can be used to provide a more detailed description based on the user's input. For example, if the template is for ranged weapons and the user has provided a range value of 10, the long description could be 'Creates a filter that includes all ranged weapons with a range above 10'</param>
        /// <returns>The long description of the template.</returns>
        string GetLongDescription(IExposable settings);

        /// <summary>
        /// Draws the settings for this template in the given rect. This is called by the UI when the template is selected, and it should draw any necessary input fields for the user to configure the policies created by this template. The settings object returned by this method will be passed to the <see cref="IDynamicPolicyProvider"/> when it is activated, allowing it to create policies based on the user's input.
        /// </summary>
        /// <param name="rect">The rect in which to draw the settings.</param>
        /// <param name="settings">The settings object to be configured by the user. Settings should be serializable.</param>
        void DrawSettings(Rect rect, ref IExposable settings);
        /// <summary>
        /// Validates the settings object created by <see cref="DrawSettings"/>. This is called by the UI when the user tries to activate the policies created by this template, and it should return an array of error messages if the settings are invalid, or an empty array if the settings are valid. This allows the UI to show error messages to the user if they have provided invalid input, and prevent them from activating policies with invalid settings.
        /// </summary>
        /// <param name="settings">The settings object to be validated.</param>
        /// <returns>An array of error messages if the settings are invalid, or an empty array if the settings are valid.</returns>
        IEnumerable<string> ValidateSettings(IExposable settings);
        /// <summary>
        /// Creates a new instance of a <see cref="IDynamicPolicyProvider"/> based on the given settings.
        /// </summary>
        /// <param name="settings">The settings object to be used to create the policy provider.</param>
        /// <returns>A new instance of a <see cref="IDynamicPolicyProvider"/> configured based on the user's input.</returns>
        IDynamicPolicyProvider Create(IExposable settings);
    }
}
