using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Filtering;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.Configuration.Templates
{
    /// <summary>
    /// Base class for templates without input.
    /// </summary>
    public abstract class PolicyPreset : IDynamicPolicyTemplate
    {
        /// <inheritdoc/>
        public abstract string StorageKey { get; }
        /// <inheritdoc cref="IDynamicPolicyTemplate.Create(IExposable)"/>
        public abstract IDynamicPolicyProvider Create();
        /// <inheritdoc />
        public IDynamicPolicyProvider Create(IExposable settings)
            => Create();
        /// <inheritdoc/>
        public void DrawSettings(Rect rect, ref IExposable settings)
        {
            settings ??= new NullSettings();
        }
        /// <inheritdoc/>
        public string GetLongDescription(IExposable settings)
            => GetShortDescription();
        /// <inheritdoc/>
        public abstract string GetShortDescription();
        /// <inheritdoc/>
        public abstract string GetTitle();
        /// <inheritdoc/>
        public IEnumerable<string> ValidateSettings(IExposable settings)
        {
            return Array.Empty<string>();
        }

        private class NullSettings : IExposable
        {
            public void ExposeData()
            {
            }
        }
    }
}
