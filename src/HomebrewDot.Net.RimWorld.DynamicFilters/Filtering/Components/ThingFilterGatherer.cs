using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Indexing;
using RimWorld;
using Verse;
using Verse.Noise;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Filtering.Components
{
    /// <summary>
    /// Pushes <see cref="ThingFilter"/> data to the snapshot manager.
    /// </summary>
    public class ThingFilterGatherer : IDataGatherer
    {
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="ThingFilterGatherer"/>.
        /// </summary>
        public static readonly ThingFilterGatherer Instance = new ThingFilterGatherer();
        private static ISnapshotManager SnapshotManager { get; set; }
        /// <inheritdoc/>
        public void GatherData(Game game, ISnapshotManager snapshotManager)
        {
            SnapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));

            if(game != null)
            {
                Scan(game);
            }
        }
        /// <inheritdoc/>
        public void Initialize(Game game)
        {
            var harmony = DynamicFiltersToolkit.Harmony;
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_Zone_Deregister));
            var original = AccessTools.Method(typeof(Zone), nameof(Zone.Deregister));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_Zone_PostRegister));
            original = AccessTools.Method(typeof(Zone), nameof(Zone.PostRegister));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_BillStack_Delete));
            original = AccessTools.Method(typeof(BillStack), nameof(BillStack.Delete));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_BillStack_AddBill));
            original = AccessTools.Method(typeof(BillStack), nameof(BillStack.AddBill));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_Building_Storage_Notify_SettingsChanged));
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.Notify_SettingsChanged));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_Building_Storage_Destroy));
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.Destroy));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
        /// <inheritdoc/>
        public void Reset()
        {
            var harmony = DynamicFiltersToolkit.Harmony;
            var original = AccessTools.Method(typeof(Zone), nameof(Zone.Deregister));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(Zone), nameof(Zone.PostRegister));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(BillStack), nameof(BillStack.Delete));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(BillStack), nameof(BillStack.AddBill));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.Notify_SettingsChanged));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.Destroy));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
        }

        private void Scan(Game game)
        {
            // Map related
            if (game.Maps != null)
            {
                foreach (var map in game.Maps)
                {
                    if (map.zoneManager?.AllZones != null)
                    {
                        foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
                        {
                            if (zone?.settings?.filter != null)
                            {
                                var storageId = zone.GetUniqueLoadID();
                                SnapshotManager?.Push(zone.settings.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, zone), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(map), map));
                            }
                        }
                    }
                    if (map.listerBuildings?.allBuildingsColonist != null)
                    {
                        foreach (var building in map.listerBuildings.allBuildingsColonist)
                        {
                            if (building is Building_Storage buildingStorage)
                            {
                                var settings = buildingStorage.GetStoreSettings();
                                var storageId = buildingStorage.storageGroup != null ? buildingStorage.storageGroup.GetUniqueLoadID() : buildingStorage.GetUniqueLoadID();
                                SnapshotManager?.Push(settings.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, buildingStorage), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(map), map));
                            }
                            if (building is Building_WorkTable workTable && workTable?.billStack != null)
                            {
                                foreach (var bill in workTable.billStack)
                                {
                                    if (bill?.ingredientFilter != null)
                                    {
                                        var storageId = bill.GetUniqueLoadID();
                                        SnapshotManager?.Push(bill.ingredientFilter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(map), map));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Game related
            if (game?.outfitDatabase?.AllOutfits != null)
            {
                foreach (var outfit in game.outfitDatabase.AllOutfits)
                {
                    if (outfit?.filter != null)
                    {
                        var storageId = outfit.GetUniqueLoadID();
                        SnapshotManager?.Push(outfit.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, outfit), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId));
                    }
                }
            }

            if (game?.foodRestrictionDatabase?.AllFoodRestrictions != null)
            {
                foreach (var foodRestriction in game.foodRestrictionDatabase.AllFoodRestrictions)
                {
                    if (foodRestriction?.filter != null)
                    {
                        var storageId = foodRestriction.GetUniqueLoadID();
                        SnapshotManager?.Push(foodRestriction.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, foodRestriction), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId));
                    }
                }
            }
        }

        /// <summary>
        /// Harmony patches used to manage the lifecycle of the <see cref="ThingFilter"/>s pushed by the <see cref="ThingFilterGatherer"/>.
        /// </summary>
        public static class Patches
        {
            // Zone
            /// <summary>
            /// Removes the <see cref="ThingFilter"/> data from the snapshot manager when a <see cref="Zone_Stockpile"/> is deregistered.
            /// </summary>
            /// <param name="__instance">The instance of the zone being deregistered.</param>
            public static void Postfix_Zone_Deregister(Zone __instance)
            {
                if (__instance is Zone_Stockpile stockpile)
                {
                    var storageId = stockpile.GetUniqueLoadID();
                    var thingFilter = stockpile?.settings?.filter;
                    if (thingFilter != null)
                    {
                        SnapshotManager?.Destroyed(thingFilter, (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(stockpile.Map), stockpile.Map), (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, stockpile));
                    }
                }
            }
            /// <summary>
            /// Pushes the <see cref="ThingFilter"/> data to the snapshot manager when a <see cref="Zone_Stockpile"/> is registered.
            /// </summary>
            /// <param name="__instance">The instance of the zone being registered.</param>
            public static void Postfix_Zone_PostRegister(Zone __instance)
            {
                if (__instance is Zone_Stockpile stockpile)
                {
                    var storageId = stockpile.GetUniqueLoadID();
                    var thingFilter = stockpile?.settings?.filter;
                    if (thingFilter != null)
                    {
                        SnapshotManager?.Push(thingFilter, (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(stockpile.Map), stockpile.Map), (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, stockpile));
                    }
                }
            }
            // Bills
            /// <summary>
            /// Removes the <see cref="ThingFilter"/> data from the snapshot manager when a <see cref="Bill"/> is removed from a <see cref="BillStack"/>.
            /// </summary>
            /// <param name="__instance">The instance of the bill stack from which the bill is being removed.</param>
            /// <param name="bill">The bill being removed.</param>
            public static void Postfix_BillStack_Delete(BillStack __instance, Bill bill)
            {
                var thingFilter = bill?.ingredientFilter;
                if (thingFilter != null)
                {
                    var storageId = bill.GetUniqueLoadID();
                    SnapshotManager?.Destroyed(thingFilter, (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(__instance.billGiver.Map), __instance.billGiver?.Map), (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill));
                }
            }
            /// <summary>
            /// Pushes the <see cref="ThingFilter"/> data to the snapshot manager when a <see cref="Bill"/> is added to a <see cref="BillStack"/>.
            /// </summary>
            /// <param name="__instance">The instance of the bill stack to which the bill is being added.</param>
            /// <param name="bill">The bill being added.</param>
            public static void Postfix_BillStack_AddBill(BillStack __instance, Bill bill)
            {
                var thingFilter = bill?.ingredientFilter;
                if (thingFilter != null)
                {
                    var storageId = bill.GetUniqueLoadID();
                    SnapshotManager?.Push(thingFilter, (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(__instance.billGiver.Map), __instance.billGiver?.Map), (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill));
                }
            }
            /// <summary>
            /// Pushes the <see cref="ThingFilter"/> data to the snapshot manager when a <see cref="Building_Storage"/> has its settings changed.
            /// </summary>
            /// <param name="__instance">The instance of the building storage whose settings have changed.</param>
            public static void Postfix_Building_Storage_Notify_SettingsChanged(Building_Storage __instance)
            {
                var settings = __instance.GetStoreSettings();
                var storageId = __instance.storageGroup != null ? __instance.storageGroup.GetUniqueLoadID() : __instance.GetUniqueLoadID();
                SnapshotManager?.Push(settings.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(__instance.Map), __instance.Map));
            }

            /// <summary>
            /// Removes the <see cref="ThingFilter"/> data from the snapshot manager when a <see cref="Building_Storage"/> is destroyed.
            /// </summary>
            /// <param name="__instance">The instance of the building storage being destroyed.</param>
            /// <param name="mode">The mode in which the building storage is being destroyed.</param>
            public static void Postfix_Building_Storage_Destroy(Building_Storage __instance, DestroyMode mode)
            { 
                var settings = __instance.GetStoreSettings();
                var instanceSettings = __instance.settings;
                var storageId = __instance.storageGroup != null ? __instance.storageGroup.GetUniqueLoadID() : __instance.GetUniqueLoadID();
                var instanceStorageId = __instance.GetUniqueLoadID();
                SnapshotManager?.Destroyed(settings.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId), (nameof(__instance.Map), __instance.Map), (nameof(DestroyMode), mode));
                if(settings != instanceSettings)
                {
                    SnapshotManager?.Destroyed(instanceSettings.filter, (DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance), (DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, instanceStorageId), (nameof(__instance.Map), __instance.Map), (nameof(DestroyMode), mode));
                }
            }
        }
    }
}
