using HomebrewDot.Net.Rimworld.Filtering;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Configuration.Templates
{
    /// <summary>
    /// A policy that acts as a <see cref="Preset"/> but does expose a limited amount of options.
    /// </summary>
    public abstract class PolicyHelper<TPolicy, TOptions> : IDynamicPolicyTemplate
        where TPolicy : IDynamicPolicyTemplate
        where TOptions : class, IExposable
    {
        // Fields
        /// <summary>
        /// The policy being managed by this helper.
        /// </summary>
        protected readonly TPolicy _policy;

        // Properties
        /// <inheritdoc/>
        public abstract string StorageKey { get; }
        /// <inheritdoc/>
        public bool Singleton => false;

        /// <inheritdoc cref="PolicyHelper{TPolicy, TOptions}"/>
        /// <param name="policy"><inheritdoc cref="_policy"/></param>
        protected PolicyHelper(TPolicy policy)
        {
            _policy = Guard.NotNull(policy, nameof(policy));
        }

        /// <inheritdoc/>
        public IDynamicPolicyProvider Create(IExposable settings)
            => _policy.Create(ConvertOptions(settings as TOptions));

        /// <inheritdoc/>
        public abstract void DrawSettings(Rect rect, ref IExposable settings);
        /// <inheritdoc/>
        public string GetLongDescription(IExposable settings)
            => _policy.GetLongDescription(settings);
        /// <inheritdoc/>
        public virtual string GetShortDescription()
            => _policy.GetShortDescription();
        /// <inheritdoc/>
        public abstract string GetTitle();
        /// <summary>
        /// Converts <paramref name="options"/> to the settings that the underlying policy uses.
        /// </summary>
        /// <param name="options">The configured settings to convert</param>
        /// <returns>The converted settings</returns>
        public abstract IExposable ConvertOptions(TOptions options);
        /// <inheritdoc/>
        public IEnumerable<string> ValidateSettings(IExposable settings)
        {
            bool hasErrors = false;
            if (settings is not TOptions options)
            {
                yield return "Unexpected settings type";
                yield break;
            }
            foreach (var error in ValidateSettings(options))
            {
                hasErrors = true;
                yield return error;
            }

            if (!hasErrors)
            {
                foreach(var error in _policy.ValidateSettings(ConvertOptions(options)))
                {
                    yield return error;
                }
            }
        }
        /// <inheritdoc cref="ValidateSettings(IExposable)"/>
        public abstract IEnumerable<string> ValidateSettings(TOptions settings);
    }
}
