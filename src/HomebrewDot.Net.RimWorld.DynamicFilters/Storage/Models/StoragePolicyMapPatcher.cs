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
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
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

            var policyManager = MapPolicyManager.GetFor(map);
            if(policyManager == null)
            {
                return true;
            }

            bool anyFilterActive = policyManager.TryGetActiveFilters(__instance, out var activeFilter, out var activeDefFilter);
            if(!anyFilterActive)
            {
                return true;
            }

            bool thingResult = true;
            bool defResult = true;
            string thingLabel = "*";
            string defLabel = "*";
            if (activeFilter != null)
            {
                thingResult = activeFilter.Filter(t);
                thingLabel = thingResult.ToString();
            }
            if (activeDefFilter != null)
            {
                defResult = activeDefFilter.Filter(t.def);
                defLabel = defResult.ToString();
            }

            __result = thingResult && defResult;
            return __result;
        }

        private const float PolicyBarHeight = 28f;
        private const float PolicyBarGap = 4f;

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

            var manager = MapPolicyManager.GetFor(map);
            if (manager == null || !manager.CouldManage(filter))
            {
                return;
            }

            var thingPolicies = manager.GetActiveThingPolicyNames();
            var defPolicies = manager.GetActiveDefPolicyNames();
            bool hasThings = thingPolicies != null && thingPolicies.Count > 0;
            bool hasDefs = defPolicies != null && defPolicies.Count > 0;

            if (!hasThings && !hasDefs)
            {
                return;
            }

            var barsDrawn = 0;
            var cursorY = rect.y;

            // Thing filter bar
            if (hasThings)
            {
                var barRect = new Rect(rect.x, cursorY, rect.width, PolicyBarHeight);
                DrawPolicyBar(barRect, manager, filter, thingPolicies, isForThing: true);
                cursorY = barRect.yMax + PolicyBarGap;
                barsDrawn++;
            }

            // Def filter bar
            if (hasDefs)
            {
                var barRect = new Rect(rect.x, cursorY, rect.width, PolicyBarHeight);
                DrawPolicyBar(barRect, manager, filter, defPolicies, isForThing: false);
                cursorY = barRect.yMax + PolicyBarGap;
                barsDrawn++;
            }

            var totalHeight = barsDrawn * PolicyBarHeight + (barsDrawn - 1) * PolicyBarGap + PolicyBarGap;
            rect = new Rect(rect.x, rect.y + totalHeight, rect.width, Mathf.Max(0f, rect.height - totalHeight));
        }

        private static void DrawPolicyBar(Rect barRect, MapPolicyManager manager, ThingFilter filter,
            IReadOnlyCollection<string> availablePolicies, bool isForThing)
        {
            var prefix = isForThing ? "Allow" : "Select";
            manager.TryGetManagedPolicyName(filter, isForThing, out var selectedPolicyName);
            var displayName = string.IsNullOrWhiteSpace(selectedPolicyName) ? "*" : selectedPolicyName;
            var fullLabel = $"{prefix}: {displayName}";

            // Truncate if too long
            var truncatedLabel = fullLabel;
            var needsTooltip = false;
            var labelWidth = Text.CalcSize(fullLabel).x;
            var availableWidth = barRect.width - 8f;
            if (labelWidth > availableWidth)
            {
                needsTooltip = true;
                truncatedLabel = TruncateLabel(fullLabel, availableWidth);
            }

            if (Widgets.ButtonInvisible(barRect))
            {
                var options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("None", () => manager.Unmanage(filter, isForThing))
                };

                foreach (var policyName in availablePolicies)
                {
                    var capturedPolicyName = policyName;
                    options.Add(new FloatMenuOption(policyName, () => manager.ManageWith(filter, capturedPolicyName, isForThing)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            Widgets.DrawMenuSection(barRect);
            var labelRect = barRect.ContractedBy(4f);
            Widgets.Label(labelRect, truncatedLabel);

            if (needsTooltip && Mouse.IsOver(barRect))
            {
                TooltipHandler.TipRegion(barRect, fullLabel);
            }
        }

        private static string TruncateLabel(string label, float maxWidth)
        {
            if (Text.CalcSize(label).x <= maxWidth)
            {
                return label;
            }

            var ellipsis = "...";
            var ellipsisWidth = Text.CalcSize(ellipsis).x;
            var availableForText = maxWidth - ellipsisWidth;
            if (availableForText <= 0f)
            {
                return ellipsis;
            }

            // Binary search for the right truncation point
            var low = 0;
            var high = label.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                var candidate = label.Substring(0, mid) + ellipsis;
                if (Text.CalcSize(candidate).x <= maxWidth)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return label.Substring(0, low) + ellipsis;
        }

        private static Map ResolveMap(ThingFilter filter)
        {
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table == null || !table.TryFind<ThingFilter>(filter, out IIndexed<ThingFilter> indexed))
            {
                return null;
            }

            var map = indexed.GetValue<Map>(nameof(Map));
            if (map != null)
            {
                return map;
            }

            return null;
        }
    }
}
