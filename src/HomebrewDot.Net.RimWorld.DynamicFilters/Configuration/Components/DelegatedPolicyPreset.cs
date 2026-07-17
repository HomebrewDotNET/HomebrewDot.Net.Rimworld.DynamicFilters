using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Configuration.Templates;
using HomebrewDot.Net.Rimworld.Filtering;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Configuration.Components
{
    /// <summary>
    /// A preset that configures another policy.
    /// </summary>
    /// <typeparam name="T">The type of the managed policy</typeparam>
    public class DelegatedPolicyPreset<T> : Preset where T : IDynamicPolicyTemplate
    {
        // Fields
        private readonly string _name;
        private readonly string _description;
        private readonly T _policy;
        private readonly IExposable _settings;

        /// <inheritdoc cref="DelegatedPolicyPreset{T}"/>
        /// <param name="name">The name of the preset</param>
        /// <param name="description">The description of the preset</param>
        /// <param name="policy">The policy being managed</param>
        /// <param name="settings">The settings to use for the policy</param>
        public DelegatedPolicyPreset(string name, string description, T policy, IExposable settings)
        {
            _policy = Guard.NotNull(policy, nameof(policy));
            _settings = Guard.NotNull(settings, nameof(settings));
            _name = Guard.NotNullOrWhitespace(name, nameof(name));
            _description = Guard.NotNullOrWhitespace(description, nameof(description));
        }
        /// <inheritdoc/>
        public override string StorageKey => $"{_policy.StorageKey}::Preset::{_name}";
        /// <inheritdoc/>
        public override IDynamicPolicyProvider Create() => _policy.Create(_settings);
        /// <inheritdoc/>
        public override string GetLongDescription() => _policy.GetLongDescription(_settings);
        /// <inheritdoc/>
        public override string GetShortDescription() => _description;
        /// <inheritdoc/>
        public override string GetTitle() => $"[Preset] {_name}";
    }
}
