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
        private string _lockedPolicyName;
        private IDynamicPolicyTemplate _editingTemplate;
        private IExposable _workingSettings;
        private string[] _validationErrors = Array.Empty<string>();

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
            if (string.IsNullOrWhiteSpace(_lockedPolicyName))
            {
                return null;
            }

            return policies.FirstOrDefault(x => x.Name == _lockedPolicyName);
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
                var canSelect = selectedPolicy == null || selectedPolicy.Name == policy.Name;

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (selectedPolicy != null && selectedPolicy.Name == policy.Name)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 22f), policy.Name);
                if(policy.Label != policy.Name)
                    Widgets.Label(new Rect(rowRect.x + 4f, rowRect.y + 22f, rowRect.width - 8f, 24f), policy.Label);

                if (canSelect && Widgets.ButtonInvisible(rowRect))
                {
                    _lockedPolicyName = policy.Name;
                    _validationErrors = Array.Empty<string>();
                    LoadPolicyEditorState(policy.Name);
                }

                y += 52f;
            }
            Widgets.EndScrollView();
        }

        private void DrawSelectedPolicy(Rect rect, ActivatedPolicies selectedPolicy)
        {
            var editorDescription = _editingTemplate != null
                ? _editingTemplate.GetLongDescription(_workingSettings)
                : (selectedPolicy?.Description ?? string.Empty);

            var drawResult = _editorPanel.Draw(
                rect,
                _editingTemplate,
                selectedPolicy?.Title ?? string.Empty,
                editorDescription,
                ref _workingSettings,
                _validationErrors,
                selectedPolicy == null
                    ? "Select a policy from the list to view or edit it."
                    : "This policy is not template-backed, so only delete is available.");

            if (selectedPolicy == null)
            {
                return;
            }

            DrawButtons(drawResult.InnerRect, selectedPolicy, drawResult.HasTemplate, drawResult.ButtonsHeight);
        }

        private void DrawButtons(Rect innerRect, ActivatedPolicies selectedPolicy, bool showSave, float buttonsHeight)
        {
            var buttonWidth = 120f;
            var y = innerRect.yMax - buttonsHeight;

            var cancelRect = new Rect(innerRect.x, y, buttonWidth, buttonsHeight);
            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                _lockedPolicyName = null;
                _editingTemplate = null;
                _workingSettings = null;
                _validationErrors = Array.Empty<string>();
            }
            Widgets.Label(cancelRect.ContractedBy(4f), "Cancel");

            var nextX = cancelRect.xMax + 8f;
            if (showSave)
            {
                var saveRect = new Rect(nextX, y, buttonWidth, buttonsHeight);
                Widgets.DrawMenuSection(saveRect);
                if (Widgets.ButtonInvisible(saveRect))
                {
                    RequestSavePolicyEdits(selectedPolicy);
                }
                Widgets.Label(saveRect.ContractedBy(4f), "Save");
                nextX = saveRect.xMax + 8f;
            }

            var deleteRect = new Rect(nextX, y, buttonWidth, buttonsHeight);
            Widgets.DrawMenuSection(deleteRect);
            if (Widgets.ButtonInvisible(deleteRect))
            {
                Find.WindowStack.Add(new ConfirmWindow(
                    "Delete Policy",
                    $"Are you sure you want to delete policy '{selectedPolicy.Name}'?",
                    () => DeletePolicy(selectedPolicy.Name)));
            }
            Widgets.Label(deleteRect.ContractedBy(4f), "Delete");
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
            _lockedPolicyName = null;
            _editingTemplate = null;
            _workingSettings = null;
        }

        private void DeletePolicy(string policyName)
        {
            DynamicFiltersToolkit.Policies.DeactivateProvider(policyName);
            DynamicFiltersToolkit.Settings.ActiveTemplates?.RemoveAll(x => x.PolicyName == policyName);
            DynamicFiltersToolkit.Instance.WriteSettings();

            _lockedPolicyName = null;
            _editingTemplate = null;
            _workingSettings = null;
            _validationErrors = Array.Empty<string>();
        }
    }
}
