using RimWorld;
using UnityEngine;
using HomebrewDot.Net.Rimworld.UI.Settings.Tabs;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings
{
    /// <summary>
    /// Renders the Dynamic Filters settings window and coordinates tab selection.
    /// </summary>
    internal sealed class DynamicFiltersSettingsUi
    {
        private readonly IDynamicFiltersSettingsTab[] _allTabs;
        private int _selectedTabIndex;
        private int _pendingTabIndex = -1;

        /// <inheritdoc cref="DynamicFiltersSettingsUi"/>
        public DynamicFiltersSettingsUi()
        {
            _allTabs = new IDynamicFiltersSettingsTab[]
            {
                new SettingsUiTab(),
                new TemplatesUiTab(),
                new PoliciesUiTab(),
            };
        }

        /// <summary>
        /// Preselects the Policies tab the next time the settings dialog is opened.
        /// </summary>
        internal void SelectPoliciesTab()
        {
            if (TryGetTabIndex<PoliciesUiTab>(out var index))
            {
                _pendingTabIndex = index;
            }
        }

        /// <summary>
        /// Switches to the Policies tab immediately, for when the settings dialog is already open.
        /// </summary>
        internal void SelectPoliciesTabImmediately()
        {
            if (TryGetTabIndex<PoliciesUiTab>(out var index))
            {
                _pendingTabIndex = -1;
                _selectedTabIndex = index;
            }
        }

        /// <summary>
        /// Applies any pending tab selection or resets to the Settings tab. Called when the settings dialog is opened fresh.
        /// </summary>
        internal void OnSettingsDialogOpened()
        {
            if (_pendingTabIndex >= 0)
            {
                _selectedTabIndex = _pendingTabIndex;
                _pendingTabIndex = -1;
            }
            else
            {
                _selectedTabIndex = 0;
            }
        }

        private bool TryGetTabIndex<T>(out int index) where T : IDynamicFiltersSettingsTab
        {
            for (var i = 0; i < _allTabs.Length; i++)
            {
                if (_allTabs[i] is T)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Draws the full settings UI for the Dynamic Filters mod.
        /// </summary>
        /// <param name="inRect">The area available for rendering settings content.</param>
        public void Draw(Rect inRect)
        {
            if (_selectedTabIndex >= _allTabs.Length)
            {
                _selectedTabIndex = 0;
            }

            var tabsRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            DrawTabs(tabsRect);

            var contentRect = new Rect(inRect.x, tabsRect.yMax + 8f, inRect.width, Mathf.Max(0f, inRect.height - 38f));
            _allTabs[_selectedTabIndex].Draw(contentRect);
        }

        private void DrawTabs(Rect rect)
        {
            const float tabGap = 8f;
            var buttonWidth = (rect.width - (_allTabs.Length - 1) * tabGap) / _allTabs.Length;

            for (var i = 0; i < _allTabs.Length; i++)
            {
                var tabRect = new Rect(rect.x + i * (buttonWidth + tabGap), rect.y, buttonWidth, rect.height);
                Widgets.DrawMenuSection(tabRect);
                if (_selectedTabIndex == i)
                {
                    Widgets.DrawHighlightSelected(tabRect);
                }

                if (Widgets.ButtonInvisible(tabRect))
                {
                    _selectedTabIndex = i;
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tabRect, _allTabs[i].Title);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
    }
}
