using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Filtering;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.Configuration.Templates
{
    /// <summary>
    /// A policy that does not require any custom options.
    /// </summary>
    public abstract class Preset : IDynamicPolicyTemplate
    {
        /// <inheritdoc/>
        public abstract string StorageKey { get; }
        /// <inheritdoc/>
        public bool Singleton => true;

        /// <inheritdoc/>
        public IDynamicPolicyProvider Create(IExposable settings)
            => Create();
        /// <inheritdoc cref="Create(IExposable)"/>
        public abstract IDynamicPolicyProvider Create();

        /// <inheritdoc/>
        public void DrawSettings(Rect rect, ref IExposable settings)
        {
        }
        /// <inheritdoc/>
        public string GetLongDescription(IExposable settings)
            => GetLongDescription();
        /// <inheritdoc cref="GetLongDescription(IExposable)"/>
        public abstract string GetLongDescription();
        /// <inheritdoc/>
        public abstract string GetShortDescription();
        /// <inheritdoc/>
        public abstract string GetTitle();
        /// <inheritdoc/>
        public IEnumerable<string> ValidateSettings(IExposable settings)
        {
            return Array.Empty<string>();
        }


        private class PresetNullOptions : IExposable
        {
            public void ExposeData()
            {
            }
        }
    }
}
