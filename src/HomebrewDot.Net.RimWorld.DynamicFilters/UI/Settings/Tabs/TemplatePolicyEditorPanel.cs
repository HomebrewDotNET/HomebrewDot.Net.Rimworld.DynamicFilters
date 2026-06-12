using System;
using HomebrewDot.Net.Rimworld.Configuration;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    /// <summary>
    /// Shared renderer for template-backed policy editors.
    /// </summary>
    internal sealed class TemplatePolicyEditorPanel
    {
        private const float ButtonsHeight = 34f;

        internal readonly struct DrawResult
        {
            public DrawResult(bool hasTemplate, Rect innerRect)
            {
                HasTemplate = hasTemplate;
                InnerRect = innerRect;
            }

            public bool HasTemplate { get; }
            public Rect InnerRect { get; }
            public float ButtonsHeight => ButtonsHeightConst;

            private const float ButtonsHeightConst = 34f;
        }

        /// <summary>
        /// Draws a shared editor layout for template-backed settings.
        /// </summary>
        public DrawResult Draw(
            Rect rect,
            IDynamicPolicyTemplate template,
            string title,
            string description,
            ref IExposable settings,
            string[] validationErrors,
            string emptyMessage)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(8f);

            if (template == null)
            {
                Widgets.Label(innerRect, emptyMessage);
                return new DrawResult(false, innerRect);
            }

            validationErrors = validationErrors ?? Array.Empty<string>();

            var cursorY = innerRect.y;
            Widgets.Label(new Rect(innerRect.x, cursorY, innerRect.width, 24f), title);
            cursorY += 26f;

            Widgets.Label(new Rect(innerRect.x, cursorY, innerRect.width, 58f), description);
            cursorY += 62f;

            var errorsHeight = validationErrors.Length == 0 ? 0f : Mathf.Min(90f, validationErrors.Length * 22f + 4f);
            var settingsRect = new Rect(innerRect.x, cursorY, innerRect.width, Mathf.Max(80f, innerRect.height - (cursorY - innerRect.y) - ButtonsHeight - errorsHeight - 12f));

            Widgets.DrawMenuSection(settingsRect);
            template.DrawSettings(settingsRect.ContractedBy(8f), ref settings);

            cursorY = settingsRect.yMax + 6f;
            if (validationErrors.Length != 0)
            {
                GUI.color = Color.red;
                for (var i = 0; i < validationErrors.Length; i++)
                {
                    Widgets.Label(new Rect(innerRect.x, cursorY, innerRect.width, 20f), $"- {validationErrors[i]}");
                    cursorY += 20f;
                }
                GUI.color = Color.white;
            }

            return new DrawResult(true, innerRect);
        }
    }
}
