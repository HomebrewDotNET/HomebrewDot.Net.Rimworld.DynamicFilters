using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.UI.Components;
using HomebrewDot.Net.Rimworld.UI.Settings;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.DynamicFiltersToolkit.DynamicFiltersToolkitSettings;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    /// <summary>
    /// Tab for creating policies from registered templates.
    /// </summary>
    internal sealed class TemplatesUiTab : IDynamicFiltersSettingsTab
    {
        private const float ListWidthRatio = 0.42f;
        private readonly TemplatePolicyEditorPanel _editorPanel = new TemplatePolicyEditorPanel();

        private Vector2 _templateListScroll = Vector2.zero;
        private string _lockedTemplateStorageKey;
        private IExposable _workingSettings;
        private string[] _validationErrors = Array.Empty<string>();

        /// <inheritdoc/>
        public string Title => "Templates";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var templates = DynamicFiltersToolkit.Templates.All.OrderBy(t => t.GetTitle(), StringComparer.OrdinalIgnoreCase).ToList();
            var selectedTemplate = ResolveSelectedTemplate(templates);

            var leftRect = new Rect(rect.x, rect.y, rect.width * ListWidthRatio, rect.height);
            var rightRect = new Rect(leftRect.xMax + 8f, rect.y, Mathf.Max(0f, rect.width - leftRect.width - 8f), rect.height);

            DrawTemplateList(leftRect, templates, selectedTemplate);
            DrawSelectedTemplate(rightRect, selectedTemplate);
        }

        private IDynamicPolicyTemplate ResolveSelectedTemplate(List<IDynamicPolicyTemplate> templates)
        {
            if (string.IsNullOrWhiteSpace(_lockedTemplateStorageKey))
            {
                return null;
            }

            return templates.FirstOrDefault(t => t.StorageKey == _lockedTemplateStorageKey);
        }

        private void DrawTemplateList(Rect rect, List<IDynamicPolicyTemplate> templates, IDynamicPolicyTemplate selectedTemplate)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(8f);
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Available Templates");

            var listOutRect = new Rect(innerRect.x, innerRect.y + 28f, innerRect.width, Mathf.Max(0f, innerRect.height - 28f));
            var viewHeight = Mathf.Max(listOutRect.height, templates.Count * 52f + 4f);
            var listViewRect = new Rect(0f, 0f, listOutRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(listOutRect, ref _templateListScroll, listViewRect);
            var y = 0f;
            for (var i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                var rowRect = new Rect(0f, y, listViewRect.width, 48f);
                var canSelect = selectedTemplate == null || selectedTemplate.StorageKey == template.StorageKey;

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (selectedTemplate != null && selectedTemplate.StorageKey == template.StorageKey)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 22f), template.GetTitle());
                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 22f, rowRect.width - 8f, 24f), template.GetShortDescription());

                if (canSelect && Widgets.ButtonInvisible(rowRect))
                {
                    _lockedTemplateStorageKey = template.StorageKey;
                    _workingSettings = null;
                    _validationErrors = Array.Empty<string>();
                }

                y += 52f;
            }
            Widgets.EndScrollView();
        }

        private void DrawSelectedTemplate(Rect rect, IDynamicPolicyTemplate selectedTemplate)
        {
            var drawResult = _editorPanel.Draw(
                rect,
                selectedTemplate,
                selectedTemplate?.GetTitle() ?? string.Empty,
                selectedTemplate?.GetLongDescription(_workingSettings) ?? string.Empty,
                ref _workingSettings,
                _validationErrors,
                "Select a template from the list to configure and create a policy.");

            if (!drawResult.HasTemplate)
            {
                return;
            }

            var innerRect = drawResult.InnerRect;
            var buttonsHeight = drawResult.ButtonsHeight;
            var buttonWidth = 120f;
            var cancelRect = new Rect(innerRect.x, innerRect.yMax - buttonsHeight, buttonWidth, buttonsHeight);
            var saveRect = new Rect(cancelRect.xMax + 8f, cancelRect.y, buttonWidth, buttonsHeight);

            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                _lockedTemplateStorageKey = null;
                _workingSettings = null;
                _validationErrors = Array.Empty<string>();
            }
            Widgets.Label(cancelRect.ContractedBy(4f), "Cancel");

            Widgets.DrawMenuSection(saveRect);
            if (Widgets.ButtonInvisible(saveRect))
            {
                _validationErrors = UiExposableUtility.Validate(selectedTemplate, _workingSettings);
                if (_validationErrors.Length == 0)
                {
                    var suggestedName = selectedTemplate.GetTitle();
                    Find.WindowStack.Add(new PolicyNamePromptWindow(suggestedName, (name, overwrite) => SaveTemplatePolicy(selectedTemplate, name, overwrite)));
                }
            }
            Widgets.Label(saveRect.ContractedBy(4f), "Save");
        }

        private string SaveTemplatePolicy(IDynamicPolicyTemplate template, string policyName, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(policyName))
            {
                _validationErrors = new[] { "Policy name is required." };
                return _validationErrors[0];
            }

            var existingPolicy = DynamicFiltersToolkit.Policies.ActivePolicies.Any(x => x.Equals(policyName, StringComparison.OrdinalIgnoreCase));
            if (existingPolicy && !overwrite)
            {
                _validationErrors = new[] { "A policy with this name already exists. Enable overwrite to replace it." };
                return _validationErrors[0];
            }

            var description = template.GetLongDescription(_workingSettings) ?? string.Empty;
            Find.WindowStack.Add(new ConfirmWindow(
                "Confirm Policy Save",
                $"Save template changes as policy '{policyName}'?\n\n{description}",
                () => CommitTemplatePolicy(template, policyName, overwrite)));

            return null;
        }

        private void CommitTemplatePolicy(IDynamicPolicyTemplate template, string policyName, bool overwrite)
        {
            var provider = template.Create(_workingSettings);
            if (!DynamicFiltersToolkit.Policies.TryActivateProvider(policyName, provider, deactivateExisting: overwrite))
            {
                _validationErrors = new[] { "Unable to activate policy provider for this template." };
                return;
            }

            var activeTemplates = DynamicFiltersToolkit.Settings.ActiveTemplates ?? (DynamicFiltersToolkit.Settings.ActiveTemplates = new List<ActivatedTemplates>());
            activeTemplates.RemoveAll(x => x.PolicyName.Equals(policyName, StringComparison.OrdinalIgnoreCase));
            activeTemplates.Add(new ActivatedTemplates
            {
                StorageKey = template.StorageKey,
                PolicyName = policyName,
                Settings = _workingSettings,
            });

            DynamicFiltersToolkit.Instance.WriteSettings();

            _lockedTemplateStorageKey = null;
            _workingSettings = null;
            _validationErrors = Array.Empty<string>();
        }
    }
}
