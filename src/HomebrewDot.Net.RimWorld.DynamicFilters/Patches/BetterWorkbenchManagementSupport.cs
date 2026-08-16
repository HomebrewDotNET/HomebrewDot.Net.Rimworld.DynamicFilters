using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private static MethodInfo _mirrorBillsMethod;
        private static MethodInfo _billCloneMethod;

        private static FieldInfo _billConfigBillField;

        private static bool _patchesApplied;

        // Tracks the ProductAdditionalFilter instances keyed by their bill so lifecycle patches can clean up
        // the index and so the policy bar can be hidden for these def-only filters.
        private static readonly Dictionary<Bill_Production, ThingFilter> BillToFilter = new Dictionary<Bill_Production, ThingFilter>();
        private static readonly HashSet<ThingFilter> DefOnlyFilters = new HashSet<ThingFilter>();

        // Tracks bills produced by Bill.Clone so their source's policies can be transferred once the clone is
        // added to a bill stack. The clone is not on a map at clone time, so the transfer happens later from
        // the gatherer's BillStack.AddBill postfix.
        private static readonly Dictionary<Bill_Production, Bill_Production> ClonedFrom = new Dictionary<Bill_Production, Bill_Production>();

        // Per-dialog state keyed by the Dialog_ThingFilter instance, used by the close handler to tell a
        // committed working copy apart from a discarded one and to drop a cleared filter from the index.
        private static readonly ConditionalWeakTable<object, DialogThingFilterState> DialogStates = new ConditionalWeakTable<object, DialogThingFilterState>();

        private sealed class DialogThingFilterState
        {
            public ThingFilter StoredAtOpen;
        }

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
            if (_mirrorBillsMethod != null)
            {
                harmony.Patch(_mirrorBillsMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(BetterWorkbenchManagementSupport), nameof(Postfix_MirrorBills))));
            }
            if (_billCloneMethod != null)
            {
                harmony.Patch(_billCloneMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(BetterWorkbenchManagementSupport), nameof(Postfix_Bill_Production_Clone))));
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
            if (_mirrorBillsMethod != null)
            {
                harmony.Unpatch(_mirrorBillsMethod, HarmonyPatchType.Postfix, harmony.Id);
            }
            if (_billCloneMethod != null)
            {
                harmony.Unpatch(_billCloneMethod, HarmonyPatchType.Postfix, harmony.Id);
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
        /// Indexes the dialog's working copy so the policy bar resolves and edits apply to the filter the dialog
        /// commits. BWM always edits a fresh working copy and commits it on OK (assigning it when no filter exists,
        /// or copying it into the stored filter); the working copy is indexed immediately so the bar is available as
        /// soon as the dialog opens. The stored filter at open time is captured so the close handler can tell a
        /// committed working copy apart from a discarded one and can drop a cleared filter from the index.
        /// <para>
        /// The working copy is deliberately never swapped for the stored instance: on OK BWM calls
        /// <c>ProductAdditionalFilter.CopyAllowancesFrom(filter)</c>, which would copy the stored filter onto itself
        /// and clear its allowed defs (the copy clears first, then copies from the now-empty same list).
        /// </para>
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

            DialogStates.Remove(__instance);
            DialogStates.Add(__instance, new DialogThingFilterState { StoredAtOpen = _productAdditionalFilterField.GetValue(extendedBill) as ThingFilter });

            var displayed = _dialogFilterField.GetValue(__instance) as ThingFilter;
            if (displayed != null && TryResolveBill(__instance, out var bill, out var map))
            {
                PushProductAdditionalFilter(bill, map, displayed);
            }

            if (displayed != null)
            {
                DefOnlyFilters.Add(displayed);
            }
        }

        /// <summary>
        /// Cleans up the working copy when the "Count Additional" dialog closes without committing it as the
        /// stored filter, and drops a previously stored filter that was cleared on OK. The mod commits by assigning
        /// the working copy (when no filter existed) or by copying it into the stored filter; when neither happened,
        /// the working copy is temporary and must be removed from the index and registry. When the mod cleared the
        /// stored filter (all defs emptied on OK), the filter that existed when the dialog opened is no longer
        /// referenced by the bill and is dropped from the index too.
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

            DialogStates.TryGetValue(__instance, out var state);
            DialogStates.Remove(__instance);

            var displayed = _dialogFilterField.GetValue(__instance) as ThingFilter;
            if (displayed == null || !TryResolveBill(__instance, out var bill, out var map))
            {
                return;
            }

            var stored = _productAdditionalFilterField.GetValue(extendedBill) as ThingFilter;
            if (stored == displayed)
            {
                // The working copy was committed as the stored filter — keep it indexed.
                return;
            }

            // The working copy was discarded (cancelled, or a different stored filter was committed) — drop it.
            DestroyProductAdditionalFilter(bill, map, displayed);

            // A stored filter that existed at open time but was cleared on OK is no longer referenced by the
            // bill; drop it from the index so it does not linger as a stale managed filter.
            if (stored == null && state?.StoredAtOpen != null && state.StoredAtOpen != displayed)
            {
                DestroyProductAdditionalFilter(bill, map, state.StoredAtOpen);
            }
        }

        /// <summary>
        /// Re-seats the "Count Additional" output filter of the given bill after Better Workbench Management
        /// replaced it with a fresh clone. Drops the previously indexed instance (now stale) and pushes the live
        /// one so policies resolve and the policy bar shows.
        /// </summary>
        /// <param name="bill">The bill whose output filter changed.</param>
        /// <param name="map">The map the bill's work table is on.</param>
        private static void ReSeatProductAdditionalFilter(Bill_Production bill, Map map)
        {
            var live = GetProductAdditionalFilter(bill);
            if (live == null)
            {
                return;
            }
            if (BillToFilter.TryGetValue(bill, out var indexed) && !ReferenceEquals(indexed, live))
            {
                DestroyProductAdditionalFilter(bill, map, indexed);
            }
            PushProductAdditionalFilter(bill, map, live);
        }

        /// <summary>
        /// Transfers the source bill's active dynamic policies (ingredient and "Count Additional" output, def
        /// and thing) onto the destination bill, overwriting the destination's own policies. Mirrors how a
        /// manually linked storage group shares one uniform policy across all its members, so copied, pasted,
        /// and linked bills keep behaving identically.
        /// </summary>
        /// <param name="source">The bill whose policies are copied.</param>
        /// <param name="destination">The bill receiving the policies.</param>
        /// <param name="manager">The policy manager of the destination's map.</param>
        internal static void TransferPolicies(Bill_Production source, Bill_Production destination, MapPolicyManager manager)
        {
            var destinationIngredientFilter = destination.ingredientFilter;
            if (destinationIngredientFilter != null)
            {
                manager.TransferPolicy(source.GetUniqueLoadID(), destination.GetUniqueLoadID(), destinationIngredientFilter, isForThing: false);
                manager.TransferPolicy(source.GetUniqueLoadID(), destination.GetUniqueLoadID(), destinationIngredientFilter, isForThing: true);
            }

            var destinationOutputFilter = GetProductAdditionalFilter(destination);
            if (destinationOutputFilter != null)
            {
                manager.TransferPolicy(GetStorageId(source), GetStorageId(destination), destinationOutputFilter, isForThing: false);
            }
        }

        /// <summary>
        /// Keeps dynamic policies in sync when Better Workbench Management copies, pastes, or mirrors bill
        /// settings. Every BWM copy, paste, and linked-bill mirroring funnels through
        /// <c>ExtendedBillDataStorage.MirrorBills</c>, which clones the destination's "Count Additional" output
        /// filter into a new instance (leaving the index stale) and copies the ingredient allowances in place.
        /// This postfix re-seats the destination's output filter and transfers the source's policies onto the
        /// destination, overwriting, so linked bills behave like a manually linked storage group.
        /// </summary>
        /// <param name="sourceBill">The bill settings were copied from.</param>
        /// <param name="destinationBill">The bill settings were copied into.</param>
        public static void Postfix_MirrorBills(Bill_Production sourceBill, Bill_Production destinationBill)
        {
            if (sourceBill == null || destinationBill == null)
            {
                return;
            }
            var map = destinationBill.Map;
            if (map == null)
            {
                return;
            }
            var manager = MapPolicyManager.GetFor(map);
            if (manager == null)
            {
                return;
            }

            ReSeatProductAdditionalFilter(destinationBill, map);
            TransferPolicies(sourceBill, destinationBill, manager);
        }

        /// <summary>
        /// Records bills created by <see cref="Bill_Production.Clone"/> so their source's policies can be
        /// transferred when the clone is added to a bill stack. The clone is not on a map at clone time, so the
        /// transfer happens later from the gatherer's <c>BillStack.AddBill</c> postfix.
        /// </summary>
        /// <param name="__instance">The bill being cloned.</param>
        /// <param name="__result">The clone.</param>
        public static void Postfix_Bill_Production_Clone(Bill_Production __instance, Bill __result)
        {
            if (__instance == null || __result is not Bill_Production clone || ReferenceEquals(clone, __instance))
            {
                return;
            }
            ClonedFrom[clone] = __instance;
        }

        /// <summary>
        /// Transfers a cloned bill's source policies once the clone is added to a bill stack and its filters
        /// are indexed. Called from the gatherer's <c>BillStack.AddBill</c> postfix.
        /// </summary>
        /// <param name="bill">The bill that was added.</param>
        /// <param name="map">The map the bill's work table is on.</param>
        internal static void TransferClonedBillPolicies(Bill bill, Map map)
        {
            if (bill is not Bill_Production productionBill || map == null)
            {
                return;
            }
            if (!ClonedFrom.TryGetValue(productionBill, out var source))
            {
                return;
            }
            ClonedFrom.Remove(productionBill);

            var manager = MapPolicyManager.GetFor(map);
            if (manager == null)
            {
                return;
            }
            ReSeatProductAdditionalFilter(productionBill, map);
            TransferPolicies(source, productionBill, manager);
        }

        /// <summary>
        /// Drops a bill from the clone-source tracking when it is removed from a bill stack without ever being
        /// transferred, preventing the tracking dictionary from leaking.
        /// </summary>
        /// <param name="bill">The bill being removed.</param>
        internal static void RemoveClonedBill(Bill bill)
        {
            if (bill is Bill_Production productionBill)
            {
                ClonedFrom.Remove(productionBill);
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
            _mirrorBillsMethod = AccessTools.Method(_extendedBillDataStorageType, "MirrorBills");
            _billCloneMethod = AccessTools.Method(typeof(Bill_Production), "Clone");
            _billConfigBillField = AccessTools.Field(typeof(Dialog_BillConfig), "bill");

            return _mainInstanceProperty != null
                && _getExtendedBillDataStorageMethod != null
                && _getExtendedDataForMethod != null
                && _productAdditionalFilterField != null
                && _dialogFilterField != null
                && _dialogExtendedBillField != null
                && _dialogReOpenWindowField != null
                && _countProductsMethod != null
                && _billCloneMethod != null
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
