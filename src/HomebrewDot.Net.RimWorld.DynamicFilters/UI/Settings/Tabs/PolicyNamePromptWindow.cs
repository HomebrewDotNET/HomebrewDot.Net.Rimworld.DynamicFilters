using System;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    /// <summary>
    /// Popup used to gather a policy name and overwrite preference.
    /// </summary>
    internal sealed class PolicyNamePromptWindow : Window
    {
        private readonly Func<string, bool, string> _onSave;

        private string _name;
        private bool _overwrite;
        private string _error;

        public PolicyNamePromptWindow(string initialName, Func<string, bool, string> onSave)
        {
            _name = initialName ?? string.Empty;
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
        }

        public override Vector2 InitialSize => new Vector2(520f, 220f);

        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 24f), "Choose Policy Name");
            cursorY += 30f;

            _name = Widgets.TextField(new Rect(inRect.x, cursorY, inRect.width, 30f), _name);
            cursorY += 36f;

            Widgets.CheckboxLabeled(new Rect(inRect.x, cursorY, inRect.width, 24f), "Overwrite existing policy if name already exists", ref _overwrite);
            cursorY += 30f;

            if (!string.IsNullOrEmpty(_error))
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 24f), _error);
                GUI.color = Color.white;
                cursorY += 26f;
            }

            var buttonWidth = 120f;
            var cancelRect = new Rect(inRect.x, inRect.yMax - 36f, buttonWidth, 32f);
            var saveRect = new Rect(cancelRect.xMax + 8f, cancelRect.y, buttonWidth, 32f);

            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                Close();
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(cancelRect, "Cancel");
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawMenuSection(saveRect);
            if (Widgets.ButtonInvisible(saveRect))
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _error = "Policy name is required.";
                }
                else
                {
                    var saveError = _onSave(_name.Trim(), _overwrite);
                    if (string.IsNullOrEmpty(saveError))
                    {
                        Close();
                    }
                    else
                    {
                        _error = saveError;
                    }
                }
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(saveRect, "Save");
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
