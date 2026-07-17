using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Configuration.Templates;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.Policies
{
    /// <summary>
    /// Policy that filters all defs that can block a windmill.
    /// </summary>
    public class BlocksWindmillPolicy : Preset, IDynamicPolicyProvider
    {
        // Statics
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly BlocksWindmillPolicy Instance = new BlocksWindmillPolicy();

        /// <inheritdoc/>
        public override string StorageKey => $"{DynamicFiltersToolkit.ModId}.{typeof(BlocksWindmillPolicy).Name}";

        /// <inheritdoc/>
        public void Activate(string name, IDynamicPolicyProviderActivationContext context)
        {
            IDynamicPolicy<Map, ThingDef> policy = null;
            policy = new DelegateDynamicPolicy<Map, ThingDef>(name, (map) => new DelegateDynamicFilter<Map, ThingDef>(map, policy, (m, def) =>
            {
                return def.blockWind || (def.category == ThingCategory.Plant && (def.plant?.IsTree ?? false));
            }));
            context.WithLabel(GetTitle())
                .WithTitle(GetTitle())
                .WithDescription(GetShortDescription())
                .AvailableFor<Map, ThingDef>(policy);
        }
        /// <inheritdoc/>
        public void Deactivate(Action disposePolicies)
        {
        }

        /// <inheritdoc/>
        public override IDynamicPolicyProvider Create()
        {
            return this;
        }
        /// <inheritdoc/>
        public override string GetLongDescription()
            => GetShortDescription();
        /// <inheritdoc/>
        public override string GetShortDescription()
        {
            return "Filters all definitions that can block a windmill.";
        }
        /// <inheritdoc/>
        public override string GetTitle()
        {
            return "Blocks Windmill";
        }
    }
}
