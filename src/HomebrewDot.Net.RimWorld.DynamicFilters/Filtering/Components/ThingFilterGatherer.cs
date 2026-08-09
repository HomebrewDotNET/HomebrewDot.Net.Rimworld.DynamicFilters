using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Patches;
using RimWorld;
using Verse;
using Verse.Noise;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Zone = Verse.Zone;

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
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_Building_Storage_SpawnSetup));
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.SpawnSetup));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_CompAutoCut_SpawnSetup));
            original = AccessTools.Method(typeof(CompAutoCut), nameof(CompAutoCut.PostSpawnSetup));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_CompAnimalPenMarker_SpawnSetup));
            original = AccessTools.Method(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.PostSpawnSetup));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Postfix_ThingComp_DeSpawn));
            original = AccessTools.Method(typeof(ThingComp), nameof(ThingComp.PostDeSpawn));
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
            original = AccessTools.Method(typeof(Building_Storage), nameof(Building_Storage.SpawnSetup));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(CompAutoCut), nameof(CompAutoCut.PostSpawnSetup));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.PostSpawnSetup));
            harmony.Unpatch(original, HarmonyPatchType.Postfix, DynamicFiltersToolkit.Harmony.Id);
            original = AccessTools.Method(typeof(ThingComp), nameof(ThingComp.PostDeSpawn));
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
                                var metadata = new IndexMetadata();
                                metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, zone);
                                metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                                metadata.Set(ToolkitConstants.Thing.Map, map);
                                SnapshotManager?.Push(zone.settings.filter, ref metadata);
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
                                var metadata = new IndexMetadata();
                                metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, buildingStorage);
                                metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                                metadata.Set(ToolkitConstants.Thing.Map, map);
                                SnapshotManager?.Push(settings.filter, ref metadata);
                            }
                            if (building is Building_WorkTable workTable && workTable?.billStack != null)
                            {
                                foreach (var bill in workTable.billStack)
                                {
                                    if (bill?.ingredientFilter != null)
                                    {
                                        var storageId = bill.GetUniqueLoadID(); 
                                        var metadata = new IndexMetadata();
                                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill);
                                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                                        metadata.Set(ToolkitConstants.Thing.Map, map);
                                        SnapshotManager?.Push(bill.ingredientFilter, ref metadata);
                                    }
                                    // The Better Workbench Management "Count Additional" output filter is also per-bill
                                    // and shares the same ThingFilter UI, so index it too.
                                    BetterWorkbenchManagementSupport.PushProductAdditionalFilter(bill, map);
                                }
                            }
                            // Pens and wind turbines expose their filters through the same ThingFilter UI, so index
                            // them too to make them manageable by dynamic policies.
                            if (building.TryGetComp<CompAnimalPenMarker>() is CompAnimalPenMarker penMarker)
                            {
                                PushPenMarkerFilters(penMarker);
                            }
                            else if (building.TryGetComp<CompAutoCut>() is CompAutoCut autoCut)
                            {
                                PushAutoCutFilter(autoCut);
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
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, outfit);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        SnapshotManager?.Push(outfit.filter, ref metadata);
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
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, foodRestriction);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        SnapshotManager?.Push(foodRestriction.filter, ref metadata);
                    }
                }
            }
        }

        /// <summary>
        /// Pushes the pen marker's animal and auto-cut filters to the snapshot manager so they can be managed by dynamic policies.
        /// </summary>
        /// <param name="marker">The pen marker comp whose filters to push.</param>
        private static void PushPenMarkerFilters(CompAnimalPenMarker marker)
        {
            PushPenAnimalFilter(marker);
            PushAutoCutFilter(marker);
        }
        /// <summary>
        /// Pushes the pen marker's animal <see cref="ThingFilter"/> to the snapshot manager.
        /// </summary>
        /// <param name="marker">The pen marker comp whose animal filter to push.</param>
        private static void PushPenAnimalFilter(CompAnimalPenMarker marker)
        {
            var building = marker.parent;
            var map = building?.Map;
            if (building == null || map == null)
            {
                return;
            }
            PushFilter(marker.AnimalFilter, building, $"{building.GetUniqueLoadID()}_Animals", map);
        }
        /// <summary>
        /// Pushes an auto-cut <see cref="ThingFilter"/> to the snapshot manager. Covers wind turbines and pen markers.
        /// </summary>
        /// <param name="autoCut">The auto-cut comp whose filter to push.</param>
        private static void PushAutoCutFilter(CompAutoCut autoCut)
        {
            var building = autoCut.parent;
            var map = building?.Map;
            if (building == null || map == null)
            {
                return;
            }
            PushFilter(autoCut.AutoCutFilter, building, $"{building.GetUniqueLoadID()}_AutoCut", map);
        }
        /// <summary>
        /// Pushes a <see cref="ThingFilter"/> to the snapshot manager with metadata scoped to the given storage and map.
        /// </summary>
        /// <param name="filter">The filter to push.</param>
        /// <param name="storage">The storage the filter belongs to.</param>
        /// <param name="storageId">The unique storage ID for the filter.</param>
        /// <param name="map">The map the filter is scoped to.</param>
        internal static void PushFilter(ThingFilter filter, object storage, string storageId, Map map)
        {
            if (filter == null)
            {
                return;
            }
            var metadata = new IndexMetadata();
            metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, storage);
            metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
            metadata.Set(ToolkitConstants.Thing.Map, map);
            SnapshotManager?.Push(filter, ref metadata, false);
        }
        /// <summary>
        /// Removes the pen marker's animal and auto-cut filters from the snapshot manager.
        /// </summary>
        /// <param name="marker">The pen marker comp whose filters to remove.</param>
        /// <param name="map">The map the pen marker was on.</param>
        private static void DestroyPenMarkerFilters(CompAnimalPenMarker marker, Map map)
        {
            var building = marker.parent;
            if (building == null || map == null)
            {
                return;
            }
            DestroyFilter(marker.AnimalFilter, building, $"{building.GetUniqueLoadID()}_Animals", map);
            DestroyFilter(marker.AutoCutFilter, building, $"{building.GetUniqueLoadID()}_AutoCut", map);
        }
        /// <summary>
        /// Removes an auto-cut <see cref="ThingFilter"/> from the snapshot manager. Covers wind turbines and pen markers.
        /// </summary>
        /// <param name="autoCut">The auto-cut comp whose filter to remove.</param>
        /// <param name="map">The map the comp was on.</param>
        private static void DestroyAutoCutFilter(CompAutoCut autoCut, Map map)
        {
            var building = autoCut.parent;
            if (building == null || map == null)
            {
                return;
            }
            DestroyFilter(autoCut.AutoCutFilter, building, $"{building.GetUniqueLoadID()}_AutoCut", map);
        }
        /// <summary>
        /// Removes a <see cref="ThingFilter"/> from the snapshot manager using metadata scoped to the given storage and map.
        /// </summary>
        /// <param name="filter">The filter to remove.</param>
        /// <param name="storage">The storage the filter belongs to.</param>
        /// <param name="storageId">The unique storage ID for the filter.</param>
        /// <param name="map">The map the filter is scoped to.</param>
        internal static void DestroyFilter(ThingFilter filter, object storage, string storageId, Map map)
        {
            if (filter == null)
            {
                return;
            }
            var metadata = new IndexMetadata();
            metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, storage);
            metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
            metadata.Set(ToolkitConstants.Thing.Map, map);
            SnapshotManager?.Destroyed(filter, ref metadata, false);
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
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, stockpile);
                        metadata.Set(ToolkitConstants.Thing.Map, stockpile.Map);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        SnapshotManager?.Destroyed(thingFilter, ref metadata, false);
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
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, stockpile);
                        metadata.Set(ToolkitConstants.Thing.Map, stockpile.Map);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        SnapshotManager?.Push(thingFilter, ref metadata, false);
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
                    var metadata = new IndexMetadata();
                    metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill);
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.billGiver.Map);
                    metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                    SnapshotManager?.Destroyed(thingFilter, ref metadata, false);
                }
                BetterWorkbenchManagementSupport.DestroyProductAdditionalFilter(bill, __instance.billGiver.Map);
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
                    var metadata = new IndexMetadata();
                    metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, bill);
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.billGiver.Map);
                    metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                    SnapshotManager?.Push(thingFilter, ref metadata, false);
                }
                BetterWorkbenchManagementSupport.PushProductAdditionalFilter(bill, __instance.billGiver.Map);
            }
            /// <summary>
            /// Pushes the <see cref="ThingFilter"/> data to the snapshot manager when a <see cref="Building_Storage"/> has its settings changed.
            /// </summary>
            /// <param name="__instance">The instance of the building storage whose settings have changed.</param>
            public static void Postfix_Building_Storage_Notify_SettingsChanged(Building_Storage __instance)
            {
                var settings = __instance.GetStoreSettings();
                var storageId = __instance.storageGroup != null ? __instance.storageGroup.GetUniqueLoadID() : __instance.GetUniqueLoadID();
                var metadata = new IndexMetadata();
                metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance);
                metadata.Set(ToolkitConstants.Thing.Map, __instance.Map);
                metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                SnapshotManager?.Push(settings.filter, ref metadata, false);
            }

            /// <summary>
            /// Removes the <see cref="ThingFilter"/> data from the snapshot manager when a <see cref="Building_Storage"/> is destroyed.
            /// </summary>
            /// <param name="__instance">The instance of the building storage being destroyed.</param>
            /// <param name="mode">The mode in which the building storage is being destroyed.</param>
            public static void Postfix_Building_Storage_Destroy(Building_Storage __instance, DestroyMode mode)
            {
                var isGrouped = __instance.storageGroup != null;
                var instanceSettings = __instance.settings;

                if (isGrouped)
                {
                    // Building is part of a storage group — NEVER destroy the group's ThingFilter
                    // because other group members still need it in the index.
                    // Only destroy this building's own filter if it has settings separate from the group.
                    var groupSettings = __instance.GetStoreSettings();
                    if (instanceSettings != groupSettings && instanceSettings?.filter != null)
                    {
                        var storageId = __instance.GetUniqueLoadID();
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance);
                        metadata.Set(ToolkitConstants.Thing.Map, __instance.Map);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        metadata.Set(ToolkitConstants.Thing.DestroyMode, mode);
                        SnapshotManager?.Destroyed(instanceSettings.filter, ref metadata, false);
                    }
                }
                else
                {
                    // Standalone building — destroy its filter
                    var storageId = __instance.GetUniqueLoadID();
                    if (instanceSettings?.filter != null)
                    {
                        var metadata = new IndexMetadata();
                        metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance);
                        metadata.Set(ToolkitConstants.Thing.Map, __instance.Map);
                        metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                        metadata.Set(ToolkitConstants.Thing.DestroyMode, mode);
                        SnapshotManager?.Destroyed(instanceSettings.filter, ref metadata, false);
                    }
                }
            }
            /// <summary>
            /// Pushes the <see cref="ThingFilter"/> data to the snapshot manager when a <see cref="Building_Storage"/> is spawned.
            /// This ensures newly constructed storage buildings get their filter indexed so the policy bar appears.
            /// </summary>
            /// <param name="__instance">The instance of the building storage being spawned.</param>
            /// <param name="map">The map the building is being spawned on.</param>
            /// <param name="respawningAfterLoad">Whether the building is respawning after a save load.</param>
            public static void Postfix_Building_Storage_SpawnSetup(Building_Storage __instance, Map map, bool respawningAfterLoad)
            {
                // On save load, the initial Scan already indexes all buildings, so skip to avoid duplicate work.
                if (respawningAfterLoad)
                {
                    return;
                }

                var settings = __instance.GetStoreSettings();
                var storageId = __instance.storageGroup != null ? __instance.storageGroup.GetUniqueLoadID() : __instance.GetUniqueLoadID();
                if (settings?.filter != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set<object>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey, __instance);
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.Map);
                    metadata.Set(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, storageId);
                    SnapshotManager?.Push(settings.filter, ref metadata, false);
                }
            }
            /// <summary>
            /// Pushes the auto-cut <see cref="ThingFilter"/> to the snapshot manager when a <see cref="CompAutoCut"/> is spawned.
            /// Covers wind turbines (<see cref="CompAutoCutWindTurbine"/>) and pen markers (<see cref="CompAnimalPenMarker"/>, via its base call).
            /// </summary>
            /// <param name="__instance">The auto-cut comp being spawned.</param>
            /// <param name="respawningAfterLoad">Whether the comp is respawning after a save load.</param>
            public static void Postfix_CompAutoCut_SpawnSetup(CompAutoCut __instance, bool respawningAfterLoad)
            {
                // On save load, the initial Scan already indexes all buildings, so skip to avoid duplicate work.
                if (respawningAfterLoad)
                {
                    return;
                }
                PushAutoCutFilter(__instance);
            }
            /// <summary>
            /// Pushes the pen marker's animal <see cref="ThingFilter"/> to the snapshot manager when a <see cref="CompAnimalPenMarker"/> is spawned.
            /// The auto-cut filter is pushed by <see cref="Postfix_CompAutoCut_SpawnSetup"/> through the base call.
            /// </summary>
            /// <param name="__instance">The pen marker comp being spawned.</param>
            /// <param name="respawningAfterLoad">Whether the comp is respawning after a save load.</param>
            public static void Postfix_CompAnimalPenMarker_SpawnSetup(CompAnimalPenMarker __instance, bool respawningAfterLoad)
            {
                // On save load, the initial Scan already indexes all buildings, so skip to avoid duplicate work.
                if (respawningAfterLoad)
                {
                    return;
                }
                PushPenAnimalFilter(__instance);
            }
            /// <summary>
            /// Removes the <see cref="ThingFilter"/> data from the snapshot manager when a <see cref="CompAutoCut"/> or <see cref="CompAnimalPenMarker"/> is despawned.
            /// This runs for all comps; only auto-cut comps are handled.
            /// </summary>
            /// <param name="__instance">The comp being despawned.</param>
            /// <param name="map">The map the comp was on.</param>
            public static void Postfix_ThingComp_DeSpawn(ThingComp __instance, Map map)
            {
                if (__instance is CompAnimalPenMarker penMarker)
                {
                    DestroyPenMarkerFilters(penMarker, map);
                }
                else if (__instance is CompAutoCut autoCut)
                {
                    DestroyAutoCutFilter(autoCut, map);
                }
            }
        }
    }
}
