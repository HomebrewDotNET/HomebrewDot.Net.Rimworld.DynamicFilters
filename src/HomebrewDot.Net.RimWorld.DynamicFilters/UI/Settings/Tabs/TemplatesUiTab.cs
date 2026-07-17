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
        private string _selectedStorageKey;
        private string _editingStorageKey;
        private IExposable _workingSettings;
        private string[] _validationErrors = Array.Empty<string>();

        private bool IsEditing => !string.IsNullOrWhiteSpace(_editingStorageKey);

        /// <summary>
        /// Returns the name of an existing active policy for a singleton template, or null if none exists or the template is not a singleton.
        /// </summary>
        private static string GetSingletonActivePolicyName(IDynamicPolicyTemplate template)
        {
            if (template == null || !template.Singleton)
            {
                return null;
            }

            var activeTemplates = DynamicFiltersToolkit.Settings.ActiveTemplates;
            if (activeTemplates == null)
            {
                return null;
            }

            return activeTemplates
                .Where(x => x.StorageKey == template.StorageKey)
                .Select(x => x.PolicyName)
                .FirstOrDefault();
        }

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
            var lookupKey = IsEditing ? _editingStorageKey : _selectedStorageKey;
            if (string.IsNullOrWhiteSpace(lookupKey))
            {
                return null;
            }

            return templates.FirstOrDefault(t => t.StorageKey == lookupKey);
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
                var isSelected = selectedTemplate != null && selectedTemplate.StorageKey == template.StorageKey;

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 22f), template.GetTitle());
                var existingPolicyName = GetSingletonActivePolicyName(template);
                var descText = existingPolicyName != null
                    ? $"(Active: {existingPolicyName}) {template.GetShortDescription()}"
                    : template.GetShortDescription();
                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 22f, rowRect.width - 8f, 24f), descText);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (IsEditing)
                    {
                        if (isSelected)
                        {
                            // Clicking the already-editing item is a no-op
                        }
                    }
                    else
                    {
                        _selectedStorageKey = template.StorageKey;
                        _workingSettings = null;
                        _validationErrors = Array.Empty<string>();
                    }
                }

                y += 52f;
            }
            Widgets.EndScrollView();
        }

        private void DrawSelectedTemplate(Rect rect, IDynamicPolicyTemplate selectedTemplate)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(8f);

            if (selectedTemplate == null)
            {
                Widgets.Label(innerRect, "Select a template from the list to configure and create a policy.");
                return;
            }

            if (IsEditing)
            {
                DrawEditingMode(rect, selectedTemplate, innerRect);
            }
            else
            {
                DrawReadOnlyMode(rect, selectedTemplate, innerRect);
            }
        }

        private void DrawReadOnlyMode(Rect rect, IDynamicPolicyTemplate selectedTemplate, Rect innerRect)
        {
            var description = selectedTemplate.GetShortDescription();
            _editorPanel.DrawReadOnly(rect, selectedTemplate.GetTitle(), description, string.Empty);

            var existingPolicyName = GetSingletonActivePolicyName(selectedTemplate);
            var buttonWidth = 120f;
            var buttonsHeight = 34f;
            var y = innerRect.yMax - buttonsHeight;

            if (existingPolicyName != null)
            {
                var statusRect = new Rect(innerRect.x, y, innerRect.width, buttonsHeight);
                GUI.color = Color.green;
                Widgets.Label(statusRect, $"Already active as policy '{existingPolicyName}'.");
                GUI.color = Color.white;
            }
            else
            {
                var editRect = new Rect(innerRect.x, y, buttonWidth, buttonsHeight);
                Widgets.DrawMenuSection(editRect);
                if (Widgets.ButtonInvisible(editRect))
                {
                    _editingStorageKey = selectedTemplate.StorageKey;
                    _workingSettings = null;
                    _validationErrors = Array.Empty<string>();
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(editRect, "Edit");
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawEditingMode(Rect rect, IDynamicPolicyTemplate selectedTemplate, Rect innerRect)
        {
            var drawResult = _editorPanel.Draw(
                rect,
                selectedTemplate,
                selectedTemplate.GetTitle(),
                selectedTemplate.GetLongDescription(_workingSettings),
                ref _workingSettings,
                _validationErrors,
                string.Empty);

            var buttonsHeight = drawResult.ButtonsHeight;
            var buttonWidth = 120f;
            var cancelRect = new Rect(innerRect.x, innerRect.yMax - buttonsHeight, buttonWidth, buttonsHeight);
            var saveRect = new Rect(cancelRect.xMax + 8f, cancelRect.y, buttonWidth, buttonsHeight);

            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                _editingStorageKey = null;
                _workingSettings = null;
                _validationErrors = Array.Empty<string>();
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(cancelRect, "Cancel");
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawMenuSection(saveRect);
            if (Widgets.ButtonInvisible(saveRect))
            {
                _validationErrors = UiExposableUtility.Validate(selectedTemplate, _workingSettings);
                if (_validationErrors.Length == 0)
                {
                    var existingName = GetSingletonActivePolicyName(selectedTemplate);
                    if (existingName != null)
                    {
                        _validationErrors = new[] { $"This is a singleton template and is already active as policy '{existingName}'. Only one policy can be active per singleton template." };
                    }
                    else
                    {
                        var suggestedName = selectedTemplate.GetTitle();
                        Find.WindowStack.Add(new PolicyNamePromptWindow(suggestedName, (name, overwrite) => SaveTemplatePolicy(selectedTemplate, name, overwrite)));
                    }
                }
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(saveRect, "Save");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private string SaveTemplatePolicy(IDynamicPolicyTemplate template, string policyName, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(policyName))
            {
                _validationErrors = new[] { "Policy name is required." };
                return _validationErrors[0];
            }

            if (template.Singleton)
            {
                var existingName = GetSingletonActivePolicyName(template);
                if (existingName != null && !existingName.Equals(policyName, StringComparison.OrdinalIgnoreCase))
                {
                    _validationErrors = new[] { $"This singleton template is already active as policy '{existingName}'. Delete it first before creating a new one." };
                    return _validationErrors[0];
                }
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
            if (template.Singleton)
            {
                // For singleton templates, remove any existing entry by the same StorageKey
                activeTemplates.RemoveAll(x => x.StorageKey == template.StorageKey);
            }
            else
            {
                activeTemplates.RemoveAll(x => x.PolicyName.Equals(policyName, StringComparison.OrdinalIgnoreCase));
            }
            activeTemplates.Add(new ActivatedTemplates
            {
                StorageKey = template.StorageKey,
                PolicyName = policyName,
                Settings = _workingSettings,
            });

            DynamicFiltersToolkit.Instance.WriteSettings();

            _editingStorageKey = null;
            _selectedStorageKey = null;
            _workingSettings = null;
            _validationErrors = Array.Empty<string>();
        }
    }
}
