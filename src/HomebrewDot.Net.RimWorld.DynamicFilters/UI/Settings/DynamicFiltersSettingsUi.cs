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

                Widgets.Label(tabRect.ContractedBy(4f), _allTabs[i].Title);
            }
        }
    }
}
