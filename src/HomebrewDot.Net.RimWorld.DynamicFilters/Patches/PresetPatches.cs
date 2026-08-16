using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using Verse;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using System.Text.RegularExpressions;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Comparing;

namespace HomebrewDot.Net.Rimworld.Patches
{
    /// <summary>
    /// Enabled presets based on the loaded mods.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class PresetPatches
    {
        /// <summary>
        /// Preset that contains Odyssey unique items.
        /// </summary>
        public const string UniquePreset = "Uniques";
        /// <summary>
        /// Preset that contains Queens from the Alpha Bees mod.
        /// </summary>
        public const string QueenBeePreset = "Bee Queens";
        /// <summary>
        /// Preset that contains Drones from the Alpha Bees mod.
        /// </summary>
        public const string DroneBeePreset = "Bee Drones";
        /// <summary>
        /// Preset that contains ghoul corpses from the Anomaly expansion.
        /// </summary>
        public const string GhoulCorpsePreset = "Ghoul Corpses";
        /// <summary>
        /// Preset that contains slave corpses from the Ideology expansion.
        /// </summary>
        public const string SlaveCorpsePreset = "Slave Corpses";
        /// <summary>
        /// Preset that contains unnatural corpses from the Anomaly expansion.
        /// </summary>
        public const string UnnaturalCorpsePreset = "Unnatural Corpses";

        /// <inheritdoc cref="PresetPatches"/>
        static PresetPatches()
        {
            Toolkit.Hooks.Manager.RegisterHook<OnGameLoadedTrigger>(DynamicFiltersToolkit.Instance, XmlContainer =>
            {
                if (ToolkitConstants.Odyssey.IsLoaded)
                {
                    Toolkit.Indexing.Thing.TrackIsUnique();
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        DynamicFilterPresets.CreateSimple(UniquePreset, "Filters all things that are Odyssey uniques, includes modded ones", DynamicFilterPresets.CreatePropertyCondition(ToolkitConstants.Thing.IsUnique.Name, TrueOperatorType.DefaultTypeName, null), false, isLazy: false);
                    });
                }
                if (ToolkitConstants.Anomaly.IsLoaded)
                {
                    Toolkit.Indexing.Thing.TrackIsGhoulCorpse();
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        // Ghouls are transformed humans, so their corpses share the Human corpse def and can only be
                        // identified per-instance. Lazy evaluation resolves the IsGhoulCorpse metadata per-call from the
                        // live database, so a fresh ghoul corpse is filtered immediately.
                        DynamicFilterPresets.CreateSimple(GhoulCorpsePreset, "Filters all ghoul corpses (Anomaly)", DynamicFilterPresets.CreateGhoulCorpseCondition(), false, isLazy: true);
                        // Unnatural corpses are UnnaturalCorpse instances. Lazy evaluation resolves the IsUnnaturalCorpse
                        // metadata per-call from the live database via TrackCorpseKind (called from ActivatePresets).
                        if (!DynamicFilterPresets.IsReplacedBySpecialThingFilterPreset("AllowCorpsesUnnatural"))
                        {
                            DynamicFilterPresets.CreateSimple(UnnaturalCorpsePreset, "Filters all unnatural corpses (Anomaly)", DynamicFilterPresets.CreateUnnaturalCorpseCondition(), false, isLazy: true);
                        }
                    });
                }
                if (ToolkitConstants.Ideology.IsLoaded)
                {
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        // Slave corpses are humanlike corpses of player-faction slaves. Lazy evaluation resolves the
                        // IsSlaveCorpse metadata per-call from the live database via TrackCorpseKind (called from
                        // ActivatePresets).
                        if (!DynamicFilterPresets.IsReplacedBySpecialThingFilterPreset("AllowCorpsesSlave"))
                        {
                            DynamicFilterPresets.CreateSimple(SlaveCorpsePreset, "Filters all slave corpses (Ideology)", DynamicFilterPresets.CreateSlaveCorpseCondition(), false, isLazy: true);
                        }
                    });
                }
                if (ToolkitConstants.Mods.Alpha.Bees.IsLoaded)
                {
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        DynamicFilterPresets.CreateSimple(QueenBeePreset, $"Filters all defs that are queen bees from {ToolkitConstants.Mods.Alpha.Bees.PackageId}",
                            ConditionBuilder.Build(builder =>
                                builder.Compare.Indexed(nameof(ThingDef.defName))
                                       .With.Match(BeeDefPrefixRegex)
                                       .And
                                       .Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name)
                                       .With.Match(BeeQueenRegex)
                            ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                            true);
                        DynamicFilterPresets.CreateSimple(DroneBeePreset, $"Filters all defs that are drone bees from {ToolkitConstants.Mods.Alpha.Bees.PackageId}",
                            ConditionBuilder.Build(builder =>
                                builder.Compare.Indexed(nameof(ThingDef.defName))
                                       .With.Match(BeeDefPrefixRegex)
                                       .And
                                       .Compare.Indexed(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name)
                                       .With.Match(BeeDroneRegex)
                            ).Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToArray(),
                            true);
                    });
                }
            }, true, priority: byte.MinValue);
        }

        private static readonly Regex BeeDefPrefixRegex = new Regex(@"(?i)^RB_", RegexOptions.Compiled);
        private static readonly Regex BeeQueenRegex = new Regex(@"(?i)Queen$", RegexOptions.Compiled);
        private static readonly Regex BeeDroneRegex = new Regex(@"(?i)Drone$", RegexOptions.Compiled);
    }
}
