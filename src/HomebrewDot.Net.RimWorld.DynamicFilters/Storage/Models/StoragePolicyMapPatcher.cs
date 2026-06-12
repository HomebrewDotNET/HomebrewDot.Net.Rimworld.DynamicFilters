using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.Indexing;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static Verse.ThingFilterUI;

namespace HomebrewDot.Net.Rimworld.Storage.Models
{
    /// <summary>
    /// Uses harmony patches to apply storage policies on <see cref="ThingFilter"/> scoped to a <see cref="Map"/>
    /// </summary>
    internal static class StoragePolicyMapPatcher
    {
        /// <summary>
        /// Activates harmony patches for applying storage policies on <see cref="ThingFilter"/> scoped to a <see cref="Map"/>.
        /// </summary>
        internal static void ApplyPatches()
        {
            var harmony = DynamicFiltersToolkit.Harmony;
            var prefix = AccessTools.Method(typeof(StoragePolicyMapPatcher), nameof(Prefix_ThingFilter_Allows));
            var original = AccessTools.Method(typeof(ThingFilter), nameof(ThingFilter.Allows), new Type[] { typeof(Thing) });
            harmony.Patch(original, new HarmonyMethod(prefix));

            var thingFilterUiMethod = AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow));
            if (thingFilterUiMethod != null)
            {
                var uiPrefix = AccessTools.Method(typeof(StoragePolicyMapPatcher), nameof(Prefix_ThingFilterUI_DoThingFilterConfigWindow));
                harmony.Patch(thingFilterUiMethod, prefix: new HarmonyMethod(uiPrefix));
            }
        }
        /// <summary>
        /// Deactivates harmony patches for applying storage policies on <see cref="ThingFilter"/> scoped to a <see cref="Map"/>.
        /// </summary>
        internal static void RemovePatches()
        {
            var harmony = DynamicFiltersToolkit.Harmony;
            var prefix = AccessTools.Method(typeof(StoragePolicyMapPatcher), nameof(Prefix_ThingFilter_Allows));
            var original = AccessTools.Method(typeof(ThingFilter), nameof(ThingFilter.Allows), new Type[] { typeof(Thing) });
            harmony.Unpatch(original, prefix);

            var thingFilterUiMethod = AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow));
            if (thingFilterUiMethod != null)
            {
                var uiPrefix = AccessTools.Method(typeof(StoragePolicyMapPatcher), nameof(Prefix_ThingFilterUI_DoThingFilterConfigWindow));
                harmony.Unpatch(thingFilterUiMethod, uiPrefix);
            }
        }

        /// <summary>
        /// Applies active storage policies to the thing filter. If there are any active policies that apply to the filter, the result of the filter will be determined by the policy. If there are no active policies that apply to the filter, the original result of the filter will be used.
        /// </summary>
        /// <param name="__instance">The <see cref="ThingFilter"/> instance being evaluated.</param>
        /// <param name="t">The <see cref="Thing"/> being checked against the filter.</param>
        /// <param name="__result">The result of the filter evaluation.</param>
        /// <returns>True if the original filter result should be used; otherwise, false.</returns>
        public static bool Prefix_ThingFilter_Allows(ThingFilter __instance, Thing t, ref bool __result)
        {
            __instance = Guard.NotNull(__instance, nameof(__instance));
            t = Guard.NotNull(t, nameof(t));

            var map = t.MapHeld ?? t.Map;
            if(map == null)
            {
                return true;
            }

            var policyManager = map.GetComponent<MapPolicyManager>();
            if(policyManager == null)
            {
                return true;
            }
            bool anyFilterActive = false;
            if(policyManager.TryGetActiveThingFilter(__instance, out var activeFilter))
            {
               anyFilterActive = true;
               __result = activeFilter.Filter(t);
            }
            if(!anyFilterActive && policyManager.TryGetActiveDefFilter(__instance, out var activeDefFilter))
            {
                anyFilterActive = true;
                __result = activeDefFilter.Filter(t.def);
            }
            return !anyFilterActive;
        }

        private const float PolicyBarHeight = 32f;
        private const float PolicyBarGap = 6f;

        /// <summary>
        /// Draws the dynamic policy dropdown at the top of the ThingFilter config window, then
        /// shrinks <paramref name="rect"/> downward so the original UI renders below it.
        /// </summary>
        public static void Prefix_ThingFilterUI_DoThingFilterConfigWindow(ref Rect rect, ThingFilter filter, Map map)
        {
            if (filter == null)
            {
                return;
            }

            map = map ?? ResolveMap(filter);
            if (map == null)
            {
                return;
            }

            var manager = map.GetComponent<MapPolicyManager>();
            if (manager == null || !manager.CouldManage(filter))
            {
                return;
            }

            var availablePolicies = manager.GetActivePolicyNames();
            if (availablePolicies == null || availablePolicies.Count == 0)
            {
                return;
            }

            // Draw the dropdown strip at the top of the rect.
            var barRect = new Rect(rect.x, rect.y, rect.width, PolicyBarHeight);

            manager.TryGetManagedPolicyName(filter, out var selectedPolicyName);
            var selectedLabel = string.IsNullOrWhiteSpace(selectedPolicyName)
                ? "None"
                : BuildPolicyLabel(manager, selectedPolicyName);

            if (Widgets.ButtonInvisible(barRect))
            {
                var options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("None", () => manager.Unmanage(filter))
                };

                foreach (var policyName in availablePolicies)
                {
                    var capturedPolicyName = policyName;
                    options.Add(new FloatMenuOption(BuildPolicyLabel(manager, capturedPolicyName), () => manager.ManageWith(filter, capturedPolicyName)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            Widgets.DrawMenuSection(barRect);
            Widgets.Label(barRect.ContractedBy(4f), $"Dynamic policy: {selectedLabel}");
            rect = new Rect(rect.x, rect.y + PolicyBarHeight + PolicyBarGap, rect.width, Mathf.Max(0f, rect.height - PolicyBarHeight - PolicyBarGap));
        }

        private static string BuildPolicyLabel(MapPolicyManager manager, string policyName)
        {
            var hasThing = manager.TryGetThingFilter(policyName, out _);
            var hasDef = manager.TryGetDefFilter(policyName, out _);

            if (hasThing && hasDef)
            {
                return $"{policyName} (thing|def)";
            }
            if (hasThing)
            {
                return $"{policyName} (thing)";
            }
            if (hasDef)
            {
                return $"{policyName} (def)";
            }
            return policyName;
        }

        private static Map ResolveMap(ThingFilter filter)
        {
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetTable();
            if (table == null || !table.TryFind<ThingFilter>(filter, out IIndexed<ThingFilter> indexed))
            {
                return null;
            }

            if (TryGetMetadata<Map>(indexed, "map", out var map))
            {
                return map;
            }

            return null;
        }

        private static bool TryGetMetadata<T>(IIndexed<ThingFilter> indexed, string key, out T value)
        {
            value = default(T);
            if (indexed?.Metadata == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (indexed.Metadata.TryGetValue(key, out var directValue) && directValue is T directTypedValue)
            {
                value = directTypedValue;
                return true;
            }

            return false;
        }
    }
}
