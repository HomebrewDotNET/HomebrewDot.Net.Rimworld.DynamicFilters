using HomebrewDot.Net.Rimworld;
using RimWorld;

namespace HomebrewDot.Net.Rimworld.UI
{
    /// <summary>
    /// Main button worker for the Policies toolbar button. Only visible when the corresponding
    /// setting is enabled and opens the Dynamic Filters mod settings with the Policies tab preselected.
    /// </summary>
    public sealed class PoliciesButtonWorker : MainButtonWorker
    {
        /// <inheritdoc/>
        public override bool Visible => DynamicFiltersToolkit.Settings.ShowPoliciesButton;

        /// <inheritdoc/>
        public override void Activate()
        {
            DynamicFiltersToolkit.Instance.OpenPoliciesSettings();
        }
    }
}
