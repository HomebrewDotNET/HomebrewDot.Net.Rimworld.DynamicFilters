using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Filtering.Models;
using HomebrewDot.Net.Rimworld.UI.Settings;
using HomebrewDot.Net.Rimworld.UI.Components;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Settings.Tabs
{
    /// <summary>
    /// Tab for inspecting and editing active policies.
    /// </summary>
    internal sealed class PoliciesUiTab : IDynamicFiltersSettingsTab
    {
        private const float ListWidthRatio = 0.42f;
        private readonly TemplatePolicyEditorPanel _editorPanel = new TemplatePolicyEditorPanel();

        private Vector2 _policyListScroll = Vector2.zero;
        private string _selectedPolicyName;
        private string _editingPolicyName;
        private IDynamicPolicyTemplate _editingTemplate;
        private IExposable _workingSettings;
        private string[] _validationErrors = Array.Empty<string>();

        private bool IsEditing => !string.IsNullOrWhiteSpace(_editingPolicyName);

        /// <inheritdoc/>
        public string Title => "Policies";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var policies = DynamicFiltersToolkit.Policies.ActivePoliciesInfo.Where(p => !p.IsReadOnly).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var selectedPolicy = ResolveSelectedPolicy(policies);

            var leftRect = new Rect(rect.x, rect.y, rect.width * ListWidthRatio, rect.height);
            var rightRect = new Rect(leftRect.xMax + 8f, rect.y, Mathf.Max(0f, rect.width - leftRect.width - 8f), rect.height);

            DrawPolicyList(leftRect, policies, selectedPolicy);
            DrawSelectedPolicy(rightRect, selectedPolicy);
        }

        private ActivatedPolicies ResolveSelectedPolicy(List<ActivatedPolicies> policies)
        {
            var lookupName = IsEditing ? _editingPolicyName : _selectedPolicyName;
            if (string.IsNullOrWhiteSpace(lookupName))
            {
                return null;
            }

            return policies.FirstOrDefault(x => x.Name == lookupName);
        }

        private void DrawPolicyList(Rect rect, List<ActivatedPolicies> policies, ActivatedPolicies selectedPolicy)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(8f);
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Active Policies");

            var listOutRect = new Rect(innerRect.x, innerRect.y + 28f, innerRect.width, Mathf.Max(0f, innerRect.height - 28f));
            var viewHeight = Mathf.Max(listOutRect.height, policies.Count * 52f + 4f);
            var listViewRect = new Rect(0f, 0f, listOutRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(listOutRect, ref _policyListScroll, listViewRect);
            var y = 0f;
            for (var i = 0; i < policies.Count; i++)
            {
                var policy = policies[i];
                var rowRect = new Rect(0f, y, listViewRect.width, 48f);
                var isSelected = selectedPolicy != null && selectedPolicy.Name == policy.Name;

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 22f), policy.Name);
                if(policy.Label != policy.Name)
                    Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 22f, rowRect.width - 8f, 24f), policy.Label);

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
                        _selectedPolicyName = policy.Name;
                        _validationErrors = Array.Empty<string>();
                        LoadPolicyInfo(policy.Name);
                    }
                }

                y += 52f;
            }
            Widgets.EndScrollView();
        }

        private void DrawSelectedPolicy(Rect rect, ActivatedPolicies selectedPolicy)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(8f);

            if (selectedPolicy == null)
            {
                Widgets.Label(innerRect, "Select a policy from the list to view or edit it.");
                return;
            }

            if (IsEditing)
            {
                DrawEditingMode(rect, selectedPolicy, innerRect);
            }
            else
            {
                DrawReadOnlyMode(rect, selectedPolicy, innerRect);
            }
        }

        private void DrawReadOnlyMode(Rect rect, ActivatedPolicies selectedPolicy, Rect innerRect)
        {
            var description = _editingTemplate != null
                ? _editingTemplate.GetLongDescription(_workingSettings)
                : (selectedPolicy.Description ?? string.Empty);

            var title = selectedPolicy.Title ?? selectedPolicy.Name;
            _editorPanel.DrawReadOnly(rect, title, description, string.Empty);

            var buttonWidth = 120f;
            var buttonGap = 8f;
            var buttonsHeight = 34f;
            var y = innerRect.yMax - buttonsHeight;

            var buttonCount = _editingTemplate != null ? 3 : 1;
            var totalButtonWidth = buttonCount * buttonWidth + (buttonCount - 1) * buttonGap;
            var startX = innerRect.x;

            if (_editingTemplate != null)
            {
                var editRect = new Rect(startX, y, buttonWidth, buttonsHeight);
                Widgets.DrawMenuSection(editRect);
                if (Widgets.ButtonInvisible(editRect))
                {
                    _editingPolicyName = selectedPolicy.Name;
                    _validationErrors = Array.Empty<string>();
                    LoadPolicyEditorState(selectedPolicy.Name);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(editRect, "Edit");
                Text.Anchor = TextAnchor.UpperLeft;
                startX = editRect.xMax + buttonGap;

                var renameRect = new Rect(startX, y, buttonWidth, buttonsHeight);
                Widgets.DrawMenuSection(renameRect);
                if (Widgets.ButtonInvisible(renameRect))
                {
                    OpenRenamePrompt(selectedPolicy);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(renameRect, "Rename");
                Text.Anchor = TextAnchor.UpperLeft;
                startX = renameRect.xMax + buttonGap;
            }

            var deleteRect = new Rect(startX, y, buttonWidth, buttonsHeight);
            Widgets.DrawMenuSection(deleteRect);
            if (Widgets.ButtonInvisible(deleteRect))
            {
                Find.WindowStack.Add(new ConfirmWindow(
                    "Delete Policy",
                    $"Are you sure you want to delete policy '{selectedPolicy.Name}'?",
                    () => DeletePolicy(selectedPolicy.Name)));
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(deleteRect, "Delete");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawEditingMode(Rect rect, ActivatedPolicies selectedPolicy, Rect innerRect)
        {
            var editorDescription = _editingTemplate != null
                ? _editingTemplate.GetLongDescription(_workingSettings)
                : (selectedPolicy.Description ?? string.Empty);

            var drawResult = _editorPanel.Draw(
                rect,
                _editingTemplate,
                selectedPolicy.Title ?? selectedPolicy.Name,
                editorDescription,
                ref _workingSettings,
                _validationErrors,
                string.Empty);

            var buttonWidth = 120f;
            var buttonsHeight = drawResult.ButtonsHeight;
            var y = innerRect.yMax - buttonsHeight;

            var cancelRect = new Rect(innerRect.x, y, buttonWidth, buttonsHeight);
            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                CancelEditing();
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(cancelRect, "Cancel");
            Text.Anchor = TextAnchor.UpperLeft;

            if (drawResult.HasTemplate)
            {
                var saveRect = new Rect(cancelRect.xMax + 8f, y, buttonWidth, buttonsHeight);
                Widgets.DrawMenuSection(saveRect);
                if (Widgets.ButtonInvisible(saveRect))
                {
                    RequestSavePolicyEdits(selectedPolicy);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(saveRect, "Save");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            var deleteRect = new Rect(innerRect.xMax - buttonWidth, y, buttonWidth, buttonsHeight);
            Widgets.DrawMenuSection(deleteRect);
            if (Widgets.ButtonInvisible(deleteRect))
            {
                Find.WindowStack.Add(new ConfirmWindow(
                    "Delete Policy",
                    $"Are you sure you want to delete policy '{selectedPolicy.Name}'?",
                    () => DeletePolicy(selectedPolicy.Name)));
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(deleteRect, "Delete");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void CancelEditing()
        {
            _editingPolicyName = null;
            _workingSettings = null;
            _validationErrors = Array.Empty<string>();
            // Reload read-only info for the still-selected policy
            if (!string.IsNullOrWhiteSpace(_selectedPolicyName))
            {
                LoadPolicyInfo(_selectedPolicyName);
            }
        }

        private void LoadPolicyInfo(string policyName)
        {
            _editingTemplate = null;
            _workingSettings = null;

            var activeTemplate = DynamicFiltersToolkit.Settings.ActiveTemplates?.FirstOrDefault(x => x.PolicyName == policyName);
            if (activeTemplate == null)
            {
                return;
            }

            _editingTemplate = DynamicFiltersToolkit.Templates.All.FirstOrDefault(x => x.StorageKey == activeTemplate.StorageKey);
            if (_editingTemplate == null)
            {
                return;
            }

            _workingSettings = activeTemplate.Settings;
        }

        private void LoadPolicyEditorState(string policyName)
        {
            _editingTemplate = null;
            _workingSettings = null;

            var activeTemplate = DynamicFiltersToolkit.Settings.ActiveTemplates?.FirstOrDefault(x => x.PolicyName == policyName);
            if (activeTemplate == null)
            {
                return;
            }

            _editingTemplate = DynamicFiltersToolkit.Templates.All.FirstOrDefault(x => x.StorageKey == activeTemplate.StorageKey);
            if (_editingTemplate == null)
            {
                return;
            }

            _workingSettings = activeTemplate.Settings;
        }

        private void RequestSavePolicyEdits(ActivatedPolicies selectedPolicy)
        {
            if (selectedPolicy.IsReadOnly)
            {
                return;
            }
            if (_editingTemplate == null)
            {
                return;
            }

            _validationErrors = UiExposableUtility.Validate(_editingTemplate, _workingSettings);
            if (_validationErrors.Length != 0)
            {
                return;
            }

            var description = _editingTemplate.GetLongDescription(_workingSettings) ?? string.Empty;
            Find.WindowStack.Add(new ConfirmWindow(
                "Confirm Policy Save",
                $"Save changes to policy '{selectedPolicy.Name}'?\n\n{description}",
                () => CommitPolicyEdits(selectedPolicy)));
        }

        private void CommitPolicyEdits(ActivatedPolicies selectedPolicy)
        {
            if(selectedPolicy.IsReadOnly)
            {
                return;
            }
            if (_editingTemplate == null)
            {
                return;
            }

            var provider = _editingTemplate.Create(_workingSettings);
            if (!DynamicFiltersToolkit.Policies.TryActivateProvider(selectedPolicy.Name, provider, deactivateExisting: true))
            {
                _validationErrors = new[] { "Unable to re-activate the updated policy." };
                return;
            }

            var activeTemplate = DynamicFiltersToolkit.Settings.ActiveTemplates?.FirstOrDefault(x => x.PolicyName == selectedPolicy.Name);
            if (activeTemplate != null)
            {
                activeTemplate.Settings = _workingSettings;
            }

            DynamicFiltersToolkit.Instance.WriteSettings();
            _validationErrors = Array.Empty<string>();
            _editingPolicyName = null;
            _editingTemplate = null;
            _workingSettings = null;
            // Reload read-only info after save
            if (!string.IsNullOrWhiteSpace(_selectedPolicyName))
            {
                LoadPolicyInfo(_selectedPolicyName);
            }
        }

        private void DeletePolicy(string policyName)
        {
            DynamicFiltersToolkit.Policies.DeactivateProvider(policyName);
            DynamicFiltersToolkit.Settings.ActiveTemplates?.RemoveAll(x => x.PolicyName == policyName);
            DynamicFiltersToolkit.Instance.WriteSettings();

            _selectedPolicyName = null;
            _editingPolicyName = null;
            _editingTemplate = null;
            _workingSettings = null;
            _validationErrors = Array.Empty<string>();
        }

        private void OpenRenamePrompt(ActivatedPolicies selectedPolicy)
        {
            Find.WindowStack.Add(new PolicyNamePromptWindow(
                selectedPolicy.Name,
                (newName, overwrite) =>
                {
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        return "Policy name is required.";
                    }

                    if (string.Equals(newName, selectedPolicy.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return "New name must differ from the current name.";
                    }

                    var existingNames = DynamicFiltersToolkit.Policies.ActivePolicies;
                    if (existingNames.Contains(newName))
                    {
                        return $"A policy named '{newName}' already exists. Choose a different name.";
                    }

                    if (!DynamicFiltersToolkit.Policies.RenameProvider(selectedPolicy.Name, newName))
                    {
                        return "Failed to rename policy. Check the log for details.";
                    }

                    // Update UI state
                    _selectedPolicyName = newName;
                    _editingPolicyName = null;
                    _editingTemplate = null;
                    _workingSettings = null;
                    _validationErrors = Array.Empty<string>();

                    return null;
                }));
        }
    }
}
