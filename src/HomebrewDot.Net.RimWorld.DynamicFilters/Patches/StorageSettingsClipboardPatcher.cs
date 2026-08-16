using HarmonyLib;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Patches
{
    /// <summary>
    /// Keeps dynamic storage policies in sync when the player copies and pastes storage settings with the
    /// vanilla copy/paste gizmos (<see cref="StorageSettingsClipboard"/>). Vanilla only copies filter
    /// allowances and priority; this captures the source storage's dynamic policies when <c>Copy</c> is used
    /// and re-applies them when <c>PasteInto</c> is used, so a pasted stockpile or storage building behaves
    /// exactly like the one it was copied from.
    /// </summary>
    internal static class StorageSettingsClipboardPatcher
    {
        private static bool _patchesApplied;

        // Capture state, mirroring the vanilla clipboard's lifetime: set on Copy and kept until the next Copy
        // or until storage filtering is disabled. Null policy names mean the source storage had no policy of
        // that kind, in which case pasting removes the destination's.
        private static bool _hasSource;
        private static string _sourceStorageId;
        private static string _sourceDefPolicy;
        private static bool _sourceDefInverted;
        private static string _sourceThingPolicy;
        private static bool _sourceThingInverted;

        /// <summary>
        /// Applies the postfixes that keep dynamic policies in sync with the vanilla storage settings
        /// copy/paste gizmos.
        /// </summary>
        internal static void ApplyPatches()
        {
            if (_patchesApplied)
            {
                return;
            }

            var harmony = DynamicFiltersToolkit.Harmony;
            var copyMethod = AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.Copy));
            var pasteMethod = AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.PasteInto));
            if (copyMethod == null || pasteMethod == null)
            {
                LogWarning("Vanilla StorageSettingsClipboard could not be resolved; copy/paste policy sync is disabled.");
                return;
            }

            harmony.Patch(copyMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(StorageSettingsClipboardPatcher), nameof(Postfix_Copy))));
            harmony.Patch(pasteMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(StorageSettingsClipboardPatcher), nameof(Postfix_PasteInto))));
            _patchesApplied = true;
        }

        /// <summary>
        /// Removes the copy/paste sync postfixes and drops any captured source state.
        /// </summary>
        internal static void RemovePatches()
        {
            if (!_patchesApplied)
            {
                return;
            }
            _patchesApplied = false;
            ResetCapture();

            var harmony = DynamicFiltersToolkit.Harmony;
            var copyMethod = AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.Copy));
            if (copyMethod != null)
            {
                harmony.Unpatch(copyMethod, HarmonyPatchType.Postfix, harmony.Id);
            }
            var pasteMethod = AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.PasteInto));
            if (pasteMethod != null)
            {
                harmony.Unpatch(pasteMethod, HarmonyPatchType.Postfix, harmony.Id);
            }
        }

        /// <summary>
        /// Captures the dynamic policies bound to the storage being copied so they can be transferred on paste.
        /// </summary>
        /// <param name="s">The storage settings being copied.</param>
        public static void Postfix_Copy(StorageSettings s)
        {
            ResetCapture();
            if (s?.filter == null)
            {
                return;
            }

            var storageId = ResolveStorageId(s.filter);
            var map = ResolveMap(s.filter);
            if (storageId == null || map == null)
            {
                return;
            }

            var manager = MapPolicyManager.GetFor(map);
            if (manager == null)
            {
                return;
            }

            if (manager.TryGetPolicyForStorage(storageId, isForThing: false, out var defPolicy, out var defInverted))
            {
                _sourceDefPolicy = defPolicy;
                _sourceDefInverted = defInverted;
            }
            if (manager.TryGetPolicyForStorage(storageId, isForThing: true, out var thingPolicy, out var thingInverted))
            {
                _sourceThingPolicy = thingPolicy;
                _sourceThingInverted = thingInverted;
            }

            _sourceStorageId = storageId;
            _hasSource = true;
            if (IsVerboseEnabled) LogVerbose($"Captured dynamic policies from storage {storageId} (def: {_sourceDefPolicy ?? "none"}, thing: {_sourceThingPolicy ?? "none"}).");
        }

        /// <summary>
        /// Transfers the captured dynamic policies onto the storage settings being pasted into, overwriting the
        /// destination's own policies. When the copied storage had no policy of a kind, the destination's is
        /// removed, mirroring how the vanilla paste overwrites filter allowances.
        /// </summary>
        /// <param name="s">The storage settings being pasted into.</param>
        public static void Postfix_PasteInto(StorageSettings s)
        {
            if (!_hasSource || s?.filter == null)
            {
                return;
            }

            var destinationStorageId = ResolveStorageId(s.filter);
            var map = ResolveMap(s.filter);
            if (destinationStorageId == null || map == null)
            {
                return;
            }
            if (destinationStorageId == _sourceStorageId)
            {
                return;
            }

            var manager = MapPolicyManager.GetFor(map);
            if (manager == null)
            {
                return;
            }

            if (_sourceDefPolicy != null)
            {
                Invoking.Safe(() => manager.ManageWith(s.filter, _sourceDefPolicy, isForThing: false, _sourceDefInverted));
            }
            else
            {
                Invoking.Safe(() => manager.Unmanage(s.filter, isForThing: false));
            }
            if (_sourceThingPolicy != null)
            {
                Invoking.Safe(() => manager.ManageWith(s.filter, _sourceThingPolicy, isForThing: true, _sourceThingInverted));
            }
            else
            {
                Invoking.Safe(() => manager.Unmanage(s.filter, isForThing: true));
            }
        }

        private static void ResetCapture()
        {
            _hasSource = false;
            _sourceStorageId = null;
            _sourceDefPolicy = null;
            _sourceDefInverted = false;
            _sourceThingPolicy = null;
            _sourceThingInverted = false;
        }

        private static string ResolveStorageId(ThingFilter filter)
        {
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table == null || filter == null)
            {
                return null;
            }
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                return indexed.GetValue<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name);
            }
            return null;
        }

        private static Map ResolveMap(ThingFilter filter)
        {
            var table = DynamicFiltersToolkit.Indexing.ThingFilter.GetCurrentTable();
            if (table == null || filter == null)
            {
                return null;
            }
            if (table.TryFind<ThingFilter>(filter, out var indexed))
            {
                return indexed.GetValue<Map>(ToolkitConstants.Thing.Map.Name);
            }
            return null;
        }
    }
}
