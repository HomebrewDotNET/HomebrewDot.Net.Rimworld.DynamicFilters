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
                        DynamicFilterPresets.CreateSimple(UniquePreset, "Filters all things that are Odyssey uniques, includes modded ones", DynamicFilterPresets.CreatePropertyCondition(ToolkitConstants.Thing.IsUnique.Name, TrueOperatorType.DefaultTypeName, null), false, false);
                    });
                }
                if (ToolkitConstants.Mods.Alpha.Bees.IsLoaded)
                {
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        DynamicFilterPresets.CreateSimple(QueenBeePreset, $"Filters all defs that are queen bees from {ToolkitConstants.Mods.Alpha.Bees.PackageId}", DynamicFilterPresets.CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name, MatchOperatorType.DefaultTypeName, BeeQueenRegex), false, false);
                        DynamicFilterPresets.CreateSimple(DroneBeePreset, $"Filters all defs that are drone bees from {ToolkitConstants.Mods.Alpha.Bees.PackageId}", DynamicFilterPresets.CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name, MatchOperatorType.DefaultTypeName, BeeDroneRegex), false, false);
                    });
                }
            }, true, priority: byte.MinValue);
        }

        private static readonly Regex BeeQueenRegex = new Regex(@"(?i)Queen$", RegexOptions.Compiled);
        private static readonly Regex BeeDroneRegex = new Regex(@"(?i)Drone$", RegexOptions.Compiled);
    }
}
