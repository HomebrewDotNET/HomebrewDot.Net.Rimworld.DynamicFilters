# Files

- [Shared UI Components](components.md) - TemplatePolicyEditorPanel (shared read-only/editing layout for template settings), PolicyNamePromptWindow (name + overwrite popup), and UiExposableUtility (Scribe-based clone and validation helper).
- [Policies Tab](policies-tab.md) - PoliciesUiTab, the settings tab listing active non-read-only policies with edit, rename, and delete flows backed by the Policies registry and the persisted ActiveTemplates entries.
- [Settings UI](settings.md) - The tabbed settings dialog shell (DynamicFiltersSettingsUi with Settings/Templates/Policies tabs), the SettingsUiTab checkboxes and failed-policy cleanup, and the optional Policies toolbar button (PoliciesButtonWorker + MainButtonDef).
- [Templates Tab](templates-tab.md) - TemplatesUiTab, the settings tab where the player picks a registered policy template, configures its settings through IDynamicPolicyTemplate.DrawSettings, validates, names, and commits a new policy.
