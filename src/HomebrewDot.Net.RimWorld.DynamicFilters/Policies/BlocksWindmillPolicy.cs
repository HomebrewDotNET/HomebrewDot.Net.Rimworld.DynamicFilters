using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Configuration.Templates;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using RimWorld;
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
            policy = new DelegateDynamicPolicy<Map, ThingDef>(name, (map) => new DelegateDynamicFilter<Map, ThingDef>(map, policy, (m, def) => BlocksWind(def)));
            context.WithLabel(GetTitle())
                .WithTitle(GetTitle())
                .WithDescription(GetShortDescription())
                .AvailableFor<Map, ThingDef>(policy);
        }
        /// <inheritdoc/>
        public void Deactivate(Action disposePolicies)
        {
        }

        /// <summary>
        /// Returns whether the given def can block a windmill. Matches things with <see cref="ThingDef.blockWind"/> set,
        /// plus all plants the game counts as trees. Covers vanilla trees (<see cref="PlantProperties.IsTree"/>) and modded
        /// trees that only set a <see cref="TreeCategory"/> (e.g. Alpha Bees' hive trees, which use
        /// <c>harvestTag</c> Standard and no <c>forceIsTree</c> so <see cref="PlantProperties.IsTree"/> is false).
        /// </summary>
        /// <param name="def">The def to check.</param>
        /// <returns><c>true</c> if the def can block a windmill; otherwise, <c>false</c>.</returns>
        public static bool BlocksWind(ThingDef def)
        {
            return def.blockWind
                || (def.category == ThingCategory.Plant
                    && def.plant != null
                    && (def.plant.IsTree || def.plant.treeCategory != TreeCategory.None));
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
