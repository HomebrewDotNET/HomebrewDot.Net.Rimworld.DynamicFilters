using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.UI.Components;
using HomebrewDot.Net.Rimworld.UI.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    /// <summary>
    /// User-facing settings tab.
    /// </summary>
    internal sealed class SettingsUiTab : IDynamicFiltersSettingsTab
    {
        /// <inheritdoc/>
        public string Title => "Settings";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            var enableStorageFiltering = DynamicFiltersToolkit.Settings.EnableStorageFiltering;
            listing.CheckboxLabeled(
                "Enable storage policies and filters",
                ref enableStorageFiltering,
                "Toggle dynamic storage policy support and filtering hooks.");
            DynamicFiltersToolkit.Settings.EnableStorageFiltering = enableStorageFiltering;
            var enablePresets = DynamicFiltersToolkit.Settings.EnablePresets;
            listing.CheckboxLabeled(
                "Enable policy presets",
                ref enablePresets,
                "Enables policy presets with common use cases.");
            DynamicFiltersToolkit.Settings.EnablePresets = enablePresets;

            // Only offered while presets are enabled: special thing filter presets are part of the preset catalog.
            if (DynamicFiltersToolkit.Settings.EnablePresets)
            {
                var enableSpecialThingFilterPresets = DynamicFiltersToolkit.Settings.EnableSpecialThingFilterPresets;
                listing.CheckboxLabeled(
                    "Enable special thing filter presets",
                    ref enableSpecialThingFilterPresets,
                    "Adds a read-only preset for every special thing filter the game loads — the stockpile \"Allow ...\" checkboxes such as allow fresh, allow rotten, allow smeltable, allow clean apparel, or allow colonist corpses — including filters added by other mods. Each preset uses the exact same check as the game's own filter. Built-in presets that duplicate a special thing filter (for example the Rotting preset, which allow rotten covers) are skipped in favour of the special thing filter preset. Activating a preset registers a collection, so the Complex Filter Policy can include or exclude it. Takes effect immediately.");
                DynamicFiltersToolkit.Settings.EnableSpecialThingFilterPresets = enableSpecialThingFilterPresets;
            }

            var showPoliciesButton = DynamicFiltersToolkit.Settings.ShowPoliciesButton;
            listing.CheckboxLabeled(
                "Show Policies button in toolbar",
                ref showPoliciesButton,
                "Adds a Policies button to the bottom toolbar that opens the mod settings with the Policies tab selected.");
            DynamicFiltersToolkit.Settings.ShowPoliciesButton = showPoliciesButton;

            listing.GapLine(12f);
            DrawFailedTemplateCleanup(listing);

            listing.End();
        }

        private static void DrawFailedTemplateCleanup(Listing_Standard listing)
        {
            var activeTemplates = DynamicFiltersToolkit.Settings.ActiveTemplates ?? new List<DynamicFiltersToolkit.DynamicFiltersToolkitSettings.ActivatedTemplates>();
            var failedTemplates = activeTemplates
                .Where(x => x != null && x.LoadFailed)
                .ToList();

            var buttonLabel = failedTemplates.Count == 0
                ? "Remove failed policies (none found)"
                : $"Remove failed policies ({failedTemplates.Count})";

            if (listing.ButtonText(buttonLabel) && failedTemplates.Count > 0)
            {
                Find.WindowStack.Add(new ConfirmWindow(
                    "Remove Failed Policies",
                    BuildRemovalMessage(failedTemplates),
                    () => RemoveFailedTemplates(failedTemplates)));
            }
        }

        private static string BuildRemovalMessage(List<DynamicFiltersToolkit.DynamicFiltersToolkitSettings.ActivatedTemplates> failedTemplates)
        {
            var lines = new List<string>
            {
                "The following activated policies failed to load and will be removed:",
                string.Empty,
            };

            foreach (var template in failedTemplates.OrderBy(x => x.PolicyName, StringComparer.OrdinalIgnoreCase))
            {
                var policyName = string.IsNullOrWhiteSpace(template.PolicyName) ? "<unnamed>" : template.PolicyName;
                var storageKey = string.IsNullOrWhiteSpace(template.StorageKey) ? "<unknown template>" : template.StorageKey;
                lines.Add($"- {policyName} ({storageKey})");
            }

            return string.Join("\n", lines);
        }

        private static void RemoveFailedTemplates(List<DynamicFiltersToolkit.DynamicFiltersToolkitSettings.ActivatedTemplates> failedTemplates)
        {
            var activeTemplates = DynamicFiltersToolkit.Settings.ActiveTemplates;
            if (activeTemplates == null || failedTemplates.Count == 0)
            {
                return;
            }

            activeTemplates.RemoveAll(x => x != null && x.LoadFailed);
            DynamicFiltersToolkit.Instance.WriteSettings();
            Messages.Message($"Removed {failedTemplates.Count} failed policy entr{(failedTemplates.Count == 1 ? "y" : "ies") }.", MessageTypeDefOf.TaskCompletion, historical: false);
        }
    }
}
