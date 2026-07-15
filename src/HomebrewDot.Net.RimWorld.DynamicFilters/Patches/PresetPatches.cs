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
        public const string UniquePreset = "Uniques Preset";
        /// <summary>
        /// Preset that contains Queens from the Alpha Bees mod.
        /// </summary>
        public const string QueenBeePreset = "Bee Queen Preset";
        /// <summary>
        /// Preset that contains Drones from the Alpha Bees mod.
        /// </summary>
        public const string DroneBeePreset = "Bee Drone Preset";

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
                        DynamicFilterPresets.ActivateSimple(UniquePreset, DynamicFilterPresets.CreatePropertyCondition(ToolkitConstants.Thing.IsUnique.Name, TrueOperatorType.DefaultTypeName, null), false, false);
                    });
                }
                if (ToolkitConstants.Mods.Alpha.Bees.IsLoaded)
                {
                    DynamicFilterPresets.AddPresetProvider(activator =>
                    {
                        DynamicFilterPresets.ActivateSimple(QueenBeePreset, DynamicFilterPresets.CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name, MatchOperatorType.DefaultTypeName, BeeQueenRegex), false, false);
                        DynamicFilterPresets.ActivateSimple(DroneBeePreset, DynamicFilterPresets.CreatePropertyCondition(Toolkit.Helpers.Expression.GetMember<ThingDef, string>(x => x.label).Name, MatchOperatorType.DefaultTypeName, BeeDroneRegex), false, false);
                    });
                }
            }, true, priority: byte.MinValue);
        }

        private static readonly Regex BeeQueenRegex = new Regex(@"(?i)Queen$", RegexOptions.Compiled);
        private static readonly Regex BeeDroneRegex = new Regex(@"(?i)Drone$", RegexOptions.Compiled);
    }
}
