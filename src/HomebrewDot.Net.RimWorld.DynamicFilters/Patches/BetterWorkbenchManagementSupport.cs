using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Patches
{
    /// <summary>
    /// Integrates the "Count Additional" output filter panel added by the Better Workbench Management mod
    /// (falconne.BWM) with dynamic storage policies.
    /// <para>
    /// The panel edits a per-bill <see cref="ThingFilter"/> stored on the mod's <c>ExtendedBillData.ProductAdditionalFilter</c>.
    /// Unlike every other filter the toolkit indexes, this one is evaluated per-def: the mod counts products by
    /// iterating <c>ProductAdditionalFilter.AllowedThingDefs</c> directly instead of calling
    /// <see cref="ThingFilter.Allows(Thing)"/>. Thing policies therefore cannot function on it, and def policies
    /// need a dedicated hook to affect the count. This class:
    /// <list type="bullet">
    /// <item><description>Makes the panel operate on the live, indexed filter instance so the policy bar resolves.</description></item>
    /// <item><description>Indexes the filter so def policies can be applied from the bar.</description></item>
    /// <item><description>Hides the thing policy bar, which is meaningless for a def-only filter.</description></item>
    /// <item><description>Applies active def policies to the count through a prefix on the mod's counting helper.</description></item>
    /// </list>
    /// Every member is resolved through reflection and every patch is applied conditionally, so this is a soft
    /// dependency: nothing runs when the mod is not active.
    /// </para>
    /// </summary>
    internal static class BetterWorkbenchManagementSupport
    {
        // Cached reflection members for the Better Workbench Management mod. Null when the mod is not loaded.
        private static Type _mainType;
        private static Type _extendedBillDataStorageType;
        private static Type _extendedBillDataType;
        private static Type _dialogThingFilterType;
        private static Type _countProductsDetourType;

        private static PropertyInfo _mainInstanceProperty;
        private static MethodInfo _getExtendedBillDataStorageMethod;
        private static MethodInfo _getExtendedDataForMethod;
        private static FieldInfo _productAdditionalFilterField;
        private static FieldInfo _dialogFilterField;
        private static FieldInfo _dialogExtendedBillField;
        private static FieldInfo _dialogReOpenWindowField;
        private static MethodInfo _countProductsMethod;

        private static FieldInfo _billConfigBillField;

        private static bool _patchesApplied;

        // Tracks the ProductAdditionalFilter instances keyed by their bill so lifecycle patches can clean up
        // the index and so the policy bar can be hidden for these def-only filters.
        private static readonly Dictionary<Bill_Production, ThingFilter> BillToFilter = new Dictionary<Bill_Production, ThingFilter>();
        private static readonly HashSet<ThingFilter> DefOnlyFilters = new HashSet<ThingFilter>();

        /// <summary>
        /// Determines whether the given filter is a def-only filter (the "Count Additional" output filter of the
        /// Better Workbench Management mod) on which thing policies cannot function.
        /// </summary>
        /// <param name="filter">The filter to check.</param>
        /// <returns>True when the filter is a def-only Better Workbench Management output filter; otherwise false.</returns>
        internal static bool IsDefOnlyFilter(ThingFilter filter)
        {
            return filter != null && DefOnlyFilters.Contains(filter);
        }

        /// <summary>
        /// Applies the Better Workbench Management integration patches. Does nothing when the mod is not active
        /// or its types cannot be resolved.
        /// </summary>
        internal static void ApplyPatches()
        {
            if (!ToolkitConstants.Mods.BetterWorkbenchManagement.IsLoaded)
            {
                return;
            }
            if (!ResolveTypes())
            {
                LogWarning("Better Workbench Management is active but its types could not be resolved; output filter integration is disabled.");
                return;
            }

            var harmony = DynamicFiltersToolkit.Harmony;
            var dialogConstructor = AccessTools.Constructor(_dialogThingFilterType, new[] { _extendedBillDataType, typeof(Window) });
            if (dialogConstructor == null)
            {
                LogWarning("Better Workbench Management: could not resolve the Dialog_ThingFilter constructor; output filter integration is disabled.");
                return;
            }
            var preCloseMethod = AccessTools.Method(_dialogThingFilterType, "PreClose");

            harmony.Patch(dialogConstructor, postfix: new HarmonyMethod(AccessTools.Method(typeof(BetterWorkbenchManagementSupport), nameof(Postfix_Dialog_ThingFilter_Constructor))));
            if (preCloseMethod != null)
            {
                harmony.Patch(preCloseMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(BetterWorkbenchManagementSupport), nameof(Postfix_Dialog_ThingFilter_PreClose))));
            }
            if (_countProductsMethod != null)
            {
                harmony.Patch(_countProductsMethod, prefix: new HarmonyMethod(AccessTools.Method(typeof(BetterWorkbenchManagementSupport), nameof(Prefix_CountProducts))));
            }

            _patchesApplied = true;
        }

        /// <summary>
        /// Removes the Better Workbench Management integration patches.
        /// </summary>
        internal static void RemovePatches()
        {
            if (!_patchesApplied)
            {
                return;
            }
            _patchesApplied = false;

            var harmony = DynamicFiltersToolkit.Harmony;
            if (_dialogThingFilterType != null)
            {
                var dialogConstructor = AccessTools.Constructor(_dialogThingFilterType, new[] { _extendedBillDataType, typeof(Window) });
                if (dialogConstructor != null)
                {
                    harmony.Unpatch(dialogConstructor, HarmonyPatchType.Postfix, harmony.Id);
                }
                var preCloseMethod = AccessTools.Method(_dialogThingFilterType, "PreClose");
                if (preCloseMethod != null)
                {
                    harmony.Unpatch(preCloseMethod, HarmonyPatchType.Postfix, harmony.Id);
                }
            }
            if (_countProductsMethod != null)
            {
                harmony.Unpatch(_countProductsMethod, HarmonyPatchType.Prefix, harmony.Id);
            }
        }

        /// <summary>
        /// Pushes the "Count Additional" output filter of the given bill to the snapshot manager when it exists.
        /// Called from the gatherer's scan and bill lifecycle patches.
        /// </summary>
        /// <param name="bill">The bill whose output filter to push.</param>
        /// <param name="map">The map the bill's work table is on.</param>
        internal static void PushProductAdditionalFilter(Bill bill, Map map)
        {
            if (bill is not Bill_Production productionBill)
            {
                return;
            }
            var filter = GetProductAdditionalFilter(productionBill);
            if (filter != null)
            {
                PushProductAdditionalFilter(productionBill, map, filter);
            }
        }

        /// <summary>
        /// Removes the "Count Additional" output filter of the given bill from the snapshot manager.
        /// Called from the gatherer's bill lifecycle patches.
        /// </summary>
        /// <param name="bill">The bill whose output filter to destroy.</param>
        /// <param name="map">The map the bill's work table is on.</param>
        internal static void DestroyProductAdditionalFilter(Bill bill, Map map)
        {
            if (bill is not Bill_Production productionBill)
            {
                return;
            }
            if (BillToFilter.TryGetValue(productionBill, out var filter))
            {
                DestroyProductAdditionalFilter(productionBill, map, filter);
            }
        }

        /// <summary>
        /// Prefix that applies active def policies to the "Count Additional" counting performed by the
        /// Better Workbench Management mod. The mod counts products by iterating the filter's allowed defs
        /// directly, never through <see cref="ThingFilter.Allows(Thing)"/>, so def policies have no effect
        /// without this hook. Products excluded by the active def policy are not counted.
        /// </summary>
        /// <param name="bill">The bill being counted.</param>
        /// <param name="productThingDef">The product def being counted.</param>
        /// <param name="defaultProduct">Whether the def is the bill's default product, which is always counted.</param>
        /// <param name="__result">The count for the def.</param>
        /// <returns>True when the original count should be used; false when <paramref name="__result"/> is final.</returns>
        public static bool Prefix_CountProducts(Bill_Production bill, ThingDef productThingDef, bool defaultProduct, ref int __result)
        {
            if (defaultProduct || bill == null || productThingDef == null)
            {
                return true;
            }

            if (!BillToFilter.TryGetValue(bill, out var filter))
            {
                filter = GetProductAdditionalFilter(bill);
                if (filter == null)
                {
                    return true;
                }
            }

            var manager = MapPolicyManager.GetFor(bill.Map);
            if (manager == null)
            {
                return true;
            }

            if (!manager.TryGetActiveFilters(filter, out _, out _, out var defFilter, out var defFilterInverted))
            {
                return true;
            }
            if (defFilter == null)
            {
                return true;
            }

            var allowed = defFilter.Filter(productThingDef);
            if (defFilterInverted)
            {
                allowed = !allowed;
            }
            if (!allowed)
            {
                __result = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Makes the "Count Additional" dialog operate on the live, indexed filter instance so the policy bar
        /// resolves and edits apply directly to the stored filter. Registers the filter as def-only so the thing
        /// policy bar is hidden. When the bill has no output filter yet, the dialog's working copy is indexed
        /// immediately so the bar is available as soon as the dialog opens.
        /// </summary>
        /// <param name="__instance">The <c>Dialog_ThingFilter</c> instance.</param>
        public static void Postfix_Dialog_ThingFilter_Constructor(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            var extendedBill = _dialogExtendedBillField.GetValue(__instance);
            if (extendedBill == null)
            {
                return;
            }

            var productFilter = _productAdditionalFilterField.GetValue(extendedBill) as ThingFilter;
            ThingFilter displayed;
            if (productFilter != null)
            {
                // Swap the working copy for the live (indexed) instance.
                _dialogFilterField.SetValue(__instance, productFilter);
                displayed = productFilter;
            }
            else
            {
                // No filter yet — keep the working copy but index it immediately so the bar shows before commit.
                displayed = _dialogFilterField.GetValue(__instance) as ThingFilter;
                if (displayed != null && TryResolveBill(__instance, out var bill, out var map))
                {
                    PushProductAdditionalFilter(bill, map, displayed);
                }
            }

            if (displayed != null)
            {
                DefOnlyFilters.Add(displayed);
            }
        }

        /// <summary>
        /// Cleans up the index and the def-only registry when the "Count Additional" dialog closes without a
        /// committed filter. The mod clears the stored filter when the def list is emptied, so the previously
        /// displayed instance must be dropped from the index.
        /// </summary>
        /// <param name="__instance">The <c>Dialog_ThingFilter</c> instance.</param>
        public static void Postfix_Dialog_ThingFilter_PreClose(object __instance)
        {
            if (__instance == null)
            {
                return;
            }
            var extendedBill = _dialogExtendedBillField.GetValue(__instance);
            if (extendedBill == null)
            {
                return;
            }

            if (_productAdditionalFilterField.GetValue(extendedBill) is ThingFilter)
            {
                return;
            }

            var displayed = _dialogFilterField.GetValue(__instance) as ThingFilter;
            if (displayed != null && TryResolveBill(__instance, out var bill, out var map))
            {
                DestroyProductAdditionalFilter(bill, map, displayed);
            }
        }

        private static bool ResolveTypes()
        {
            _mainType = AccessTools.TypeByName(ToolkitConstants.Mods.BetterWorkbenchManagement.MainTypeName);
            _extendedBillDataStorageType = AccessTools.TypeByName(ToolkitConstants.Mods.BetterWorkbenchManagement.ExtendedBillDataStorageTypeName);
            _extendedBillDataType = AccessTools.TypeByName(ToolkitConstants.Mods.BetterWorkbenchManagement.ExtendedBillDataTypeName);
            _dialogThingFilterType = AccessTools.TypeByName(ToolkitConstants.Mods.BetterWorkbenchManagement.DialogThingFilterTypeName);
            _countProductsDetourType = AccessTools.TypeByName(ToolkitConstants.Mods.BetterWorkbenchManagement.CountProductsDetourTypeName);
            if (_mainType == null || _extendedBillDataStorageType == null || _extendedBillDataType == null || _dialogThingFilterType == null || _countProductsDetourType == null)
            {
                return false;
            }

            _mainInstanceProperty = AccessTools.Property(_mainType, "Instance");
            _getExtendedBillDataStorageMethod = AccessTools.Method(_mainType, "GetExtendedBillDataStorage");
            _getExtendedDataForMethod = AccessTools.Method(_extendedBillDataStorageType, "GetExtendedDataFor");
            _productAdditionalFilterField = AccessTools.Field(_extendedBillDataType, "ProductAdditionalFilter");
            _dialogFilterField = AccessTools.Field(_dialogThingFilterType, "filter");
            _dialogExtendedBillField = AccessTools.Field(_dialogThingFilterType, "extendedBill");
            _dialogReOpenWindowField = AccessTools.Field(_dialogThingFilterType, "reOpenWindow");
            _countProductsMethod = AccessTools.Method(_countProductsDetourType, "CountProducts");
            _billConfigBillField = AccessTools.Field(typeof(Dialog_BillConfig), "bill");

            return _mainInstanceProperty != null
                && _getExtendedBillDataStorageMethod != null
                && _getExtendedDataForMethod != null
                && _productAdditionalFilterField != null
                && _dialogFilterField != null
                && _dialogExtendedBillField != null
                && _dialogReOpenWindowField != null
                && _countProductsMethod != null
                && _billConfigBillField != null;
        }

        private static ThingFilter GetProductAdditionalFilter(Bill_Production bill)
        {
            var main = _mainInstanceProperty.GetValue(null);
            if (main == null)
            {
                return null;
            }
            var storage = _getExtendedBillDataStorageMethod.Invoke(main, null);
            if (storage == null)
            {
                return null;
            }
            var extendedBill = _getExtendedDataForMethod.Invoke(storage, new object[] { bill });
            if (extendedBill == null)
            {
                return null;
            }
            return _productAdditionalFilterField.GetValue(extendedBill) as ThingFilter;
        }

        private static void PushProductAdditionalFilter(Bill_Production bill, Map map, ThingFilter filter)
        {
            if (bill == null || map == null || filter == null)
            {
                return;
            }
            ThingFilterGatherer.PushFilter(filter, bill, GetStorageId(bill), map);
            BillToFilter[bill] = filter;
            DefOnlyFilters.Add(filter);
        }

        private static void DestroyProductAdditionalFilter(Bill_Production bill, Map map, ThingFilter filter)
        {
            if (bill == null || map == null || filter == null)
            {
                return;
            }
            ThingFilterGatherer.DestroyFilter(filter, bill, GetStorageId(bill), map);
            BillToFilter.Remove(bill);
            DefOnlyFilters.Remove(filter);
        }

        private static string GetStorageId(Bill bill)
        {
            return $"{bill.GetUniqueLoadID()}_ProductAdditional";
        }

        private static bool TryResolveBill(object dialog, out Bill_Production bill, out Map map)
        {
            bill = null;
            map = null;
            var reOpenWindow = _dialogReOpenWindowField.GetValue(dialog) as Window;
            if (reOpenWindow is Dialog_BillConfig billConfig)
            {
                bill = _billConfigBillField.GetValue(billConfig) as Bill_Production;
            }
            if (bill != null)
            {
                map = bill.Map;
            }
            return bill != null && map != null;
        }
    }
}
