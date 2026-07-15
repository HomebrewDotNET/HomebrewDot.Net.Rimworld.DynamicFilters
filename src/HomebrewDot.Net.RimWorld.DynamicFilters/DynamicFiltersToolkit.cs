using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.Filtering.Models;
using HomebrewDot.Net.Rimworld.Filtering.Triggers;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using RuntimeAudioClipLoader;
using Verse;
using static HomebrewDot.Net.Rimworld.DynamicFiltersToolkit.DynamicFiltersToolkitSettings;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using HomebrewDot.Net.Rimworld.Hooks;
using UnityEngine;
using HomebrewDot.Net.Rimworld.Storage.Models;
using HomebrewDot.Net.Rimworld.UI.Settings;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using RimWorld;
using HomebrewDot.Net.Rimworld.UI;
using HomebrewDot.Net.Rimworld.UI.Components;
using HomebrewDot.Net.Rimworld.Referencing.Components;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Central access point for all the tools in the HomebrewDot.Net library related to dynamic filters.
    /// </summary>
    public class DynamicFiltersToolkit : Mod
    {
        // Statics
        private static object _lock = new object();
        private static DynamicFiltersToolkit _instance;
        private static DynamicFiltersToolkitSettings _settings;

        // Fields
        private readonly DynamicFiltersSettingsUi _settingsUi;
        
        // State
        private static bool _storageFilteringEnabled;
        private static bool _policiesLoadedFromSettings;
        private static bool _presetsActivated;

        /// <summary>
        /// The unique identifier for this mod.
        /// </summary>
        public static string ModId { get; } = "homebrewdot.net.rimworld.dynamicfilters";
        /// <summary>
        /// The Harmony instance used for patching methods.
        /// </summary>
        internal static Harmony Harmony { get; } = new Harmony(ModId);
        /// <summary>
        /// Singleton instance of the <see cref="Toolkit"/> class.
        /// </summary>
        public static DynamicFiltersToolkit Instance { get => _instance ?? throw new ArgumentNullException($"Tried to access {nameof(DynamicFiltersToolkit)} instance before it was initialized."); private set => _instance = value; }
        /// <summary>
        /// Contains the settings for the <see cref="Toolkit"/>. Accessing this property will initialize the settings if they haven't been already.
        /// </summary>
        public static DynamicFiltersToolkitSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                lock (_lock)
                {
                    if (_settings == null)
                    {
                        _settings = Instance.GetSettings<DynamicFiltersToolkitSettings>();
                    }
                }
                return _settings;
            }
        }

        /// <inhericdoc cref="DynamicFiltersToolkit">
        /// <param name="content">The mod content</param>
        public DynamicFiltersToolkit(ModContentPack content) : base(content)
        {
            Instance = this;
            _settingsUi = new DynamicFiltersSettingsUi();
            ConfigureServices();
        }

        /// <inheritdoc/>
        public override string SettingsCategory()
        {
            return "Homebrewed Dynamic Filters";
        }
        /// <inheritdoc/>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            if(!_policiesLoadedFromSettings)
            {
                if (IsVerboseEnabled) LogVerbose("Loading activated templates from settings...");
                Policies.LoadActivatedTemplates(Settings.ActiveTemplates);
                _policiesLoadedFromSettings = true;
            }
            _settingsUi.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        internal static void ConfigureServices()
        {
            Toolkit.Hooks.Manager.RegisterHook<Changed>(Instance, e =>
            {
                if (e.Settings.EnableStorageFiltering)
                {
                    EnableStorageFiltering();
                }
                else
                {
                    DisableStorageFiltering();
                }
                SetPresets(e.Settings.EnablePresets);
            }, priority: byte.MaxValue);

            if (Settings.EnableStorageFiltering)
            {
                EnableStorageFiltering();
            }
            else
            {
                DisableStorageFiltering();
            }

            Toolkit.Hooks.Manager.RegisterHook<OnSaveLoadedTrigger>(Instance, e =>
            {
                SetPresets(Settings.EnablePresets);
                if (Settings.ActiveTemplates.Count == 0)
                {
                    return;
                }
                if (!_policiesLoadedFromSettings)
                {
                    Policies.LoadActivatedTemplates(Settings.ActiveTemplates);
                    _policiesLoadedFromSettings = true;
                }
            });
        }

        private static void EnableStorageFiltering()
        {
            if(_storageFilteringEnabled) return;
            _storageFilteringEnabled = true;
            Log("Enabling storage filtering...");
            Indexing.ThingFilter.EnsureGatherer();
            Indexing.ThingFilter.EnsureTable();
            StoragePolicyMapPatcher.ApplyPatches();
            Templates.AddTemplate(BlocksWindmillPolicy.Instance);
            Templates.AddTemplate(SimpleFilterPolicy.Instance);
            Toolkit.Indexing.Indexers.BuildIndexer<Thing>(ToolkitConstants.Thing.Map.Name, x => x.Include<string>(ToolkitConstants.Thing.Map, true));
            Toolkit.Indexing.Indexers.BuildIndexer<Thing>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey.Name, x => x.Include<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, true));
            Toolkit.Indexing.Indexers.BuildIndexer<Thing>(DynamicFiltersToolkitConstants.ThingFilter.StorageKey.Name, x => x.Include<string>(DynamicFiltersToolkitConstants.ThingFilter.StorageIdKey, true));
            Toolkit.Indexing.ReloadOrchestration();
        }
        private static void DisableStorageFiltering()
        {
            if (!_storageFilteringEnabled) return;
            _storageFilteringEnabled = false;
            Log("Disabling storage filtering...");
            StoragePolicyMapPatcher.RemovePatches();
        }

        private static void SetPresets(bool enable)
        {
            if(_presetsActivated != enable)
            {
                _presetsActivated = enable;
                if(enable)
                {
                    DynamicFilterPresets.ActivatePresets();
                }
                else
                {
                    DynamicFilterPresets.DeactivatePresets();
                }
            }

        }

        /// <summary>
        /// Tools for registering templates that can be used to create dynamic policy providers. A template is a user-facing configuration for a dynamic policy provider. It allows users to create and configure policies based on predefined templates, without needing to know how to code or understand the underlying implementation of the policies. Templates can be used to create policies for specific use cases, such as filtering items based on their stats, or creating custom storage policies based on user-defined criteria.
        /// </summary>
        public static class Templates
        {
            // Fields
            private readonly static object _lock = new object();
            private readonly static HashSet<IDynamicPolicyTemplate> _templates = new HashSet<IDynamicPolicyTemplate>();

            /// <summary>
            /// Returns all available templates that can be used to create dynamic policy providers.
            /// </summary>
            public static IReadOnlyCollection<IDynamicPolicyTemplate> All
            {
                get
                {
                    lock (_lock)
                    {
                        return _templates.OrderBy(x => x.StorageKey).ToList().AsReadOnly();
                    }
                }
            }
            /// <summary>
            /// Makes a new template available for use in the toolkit.
            /// </summary>
            /// <param name="template">The dynamic policy template to be added.</param>
            public static void AddTemplate(IDynamicPolicyTemplate template)
            {
                lock (_lock)
                {
                    if (_templates.Add(template))
                    {
                        if (IsVerboseEnabled) LogVerbose($"Added dynamic policy template: {template.GetType().FullName} with storage key {template.StorageKey}");
                    }
                }
            }
        }

        /// <summary>
        /// Tools for managing policies.
        /// </summary>
        public static class Policies
        {
            // Fields
            private readonly static object _lock = new object();
            private readonly static HashSet<IDynamicPolicyProvider> _availablePolicies = new HashSet<IDynamicPolicyProvider>();
            private readonly static Dictionary<string, ActivatedPolicies> _activePolicies = new Dictionary<string, ActivatedPolicies>();

            // Properties
            /// <summary>
            /// The names of the policies that are currently active.
            /// </summary>
            public static IReadOnlyCollection<string> ActivePolicies
            {
                get
                {
                    lock (_lock)
                    {
                        return _activePolicies.Keys.OrderBy(x => x).ToList().AsReadOnly();
                    }
                }
            }
            /// <summary>
            /// Information about the policies that are currently active, including their name, label, description and the provider that activated them. This can be used to show the player which policies are currently active and to provide information about them in the UI.
            /// </summary>
            public static IReadOnlyCollection<ActivatedPolicies> ActivePoliciesInfo
            {
                get
                {
                    lock (_lock)
                    {
                        return _activePolicies.Values.OrderBy(x => x.Name).ToList().AsReadOnly();
                    }
                }
            }

            /// <summary>
            /// Adds a new dynamic policy provider to the toolkit. This allows the provider to be activated and its policies to be used in filters.
            /// </summary>
            /// <param name="provider">The dynamic policy provider to be added.</param>
            public static void AddProvider(IDynamicPolicyProvider provider)
            {
                lock (_lock)
                {
                    if (_availablePolicies.Add(provider))
                    {
                        if (IsVerboseEnabled) LogVerbose($"Added dynamic policy provider: {provider.GetType().FullName}");
                    }
                }
            }
            /// <summary>
            /// Activates a dynamic policy provider with the given name.
            /// </summary>
            /// <param name="name">The unique name of the dynamic policy provider to be activated.</param>
            /// <param name="provider">The dynamic policy provider to be activated.</param>
            /// <param name="deactivateExisting">Whether to deactivate an existing provider with the same name if it exists.</param>
            /// <param name="isReadOnly">Whether the activated provider should be read-only.</param>
            /// <returns>True if the provider was successfully activated; otherwise, false.</returns>
            public static bool TryActivateProvider(string name, IDynamicPolicyProvider provider, bool deactivateExisting = false, bool isReadOnly = false)
            {
                lock (_lock)
                {
                    if (_activePolicies.ContainsKey(name))
                    {
                        if (deactivateExisting)
                        {
                            DeactivateProvider(name);
                        }
                        else
                        {
                            LogWarning($"Tried to activate dynamic policy provider with name {name}, but a provider with that name is already active. Set {nameof(deactivateExisting)} to true to automatically deactivate the existing provider.");
                            return false;
                        }
                    }
                    var activatedPolicies = new ActivatedPolicies(name, provider, isReadOnly);
                    provider.Activate(name, activatedPolicies);
                    _activePolicies.Add(name, activatedPolicies);
                    Toolkit.Hooks.Manager.Trigger(new OnDynamicPolicyActivated(name));
                    if (IsVerboseEnabled) LogVerbose($"Activated dynamic policy provider: {provider.GetType().FullName} with name {name}");
                    return true;
                }
            }
            /// <summary>
            /// Deactivates the dynamic policy provider with the given name, if it is active. This will remove all policies provided by that provider from any filters they are used in.
            /// </summary>
            /// <param name="name">The unique name of the dynamic policy provider to be deactivated.</param>
            public static void DeactivateProvider(string name)
            {
                lock (_lock)
                {
                    if (_activePolicies.TryGetValue(name, out var activatedPolicies))
                    {
                        bool disposeCalled = false;
                        Toolkit.Hooks.Manager.Trigger(new OnDynamicPolicyDeactivated(name));
                        activatedPolicies.Provider.Deactivate(() =>
                        {
                            if (!disposeCalled)
                            {
                                disposeCalled = true;
                                activatedPolicies.Dispose();
                            }
                        });
                        if (!disposeCalled)
                        {
                            activatedPolicies.Dispose();
                        }
                        _activePolicies.Remove(name);
                        if (IsVerboseEnabled) LogVerbose($"Deactivated dynamic policy provider: {activatedPolicies.Provider.GetType().FullName} with name {name}");
                    }
                    else
                    {
                        LogWarning($"Tried to deactivate dynamic policy provider with name {name}, but no provider with that name is active.");
                    }
                }
            }

            /// <summary>
            /// Loads the activated templates from the settings and activates the corresponding providers. This should be called when the game is loaded to restore the activated policies from the previous session.
            /// </summary>
            /// <param name="templates">The list of activated templates to be loaded and activated.</param>
            public static void LoadActivatedTemplates(IEnumerable<ActivatedTemplates> templates)
            {
                lock (_lock)
                {
                    foreach (var template in templates)
                    {
                        var storageKey = template.StorageKey;
                        var provider = Templates.All.FirstOrDefault(p => p.StorageKey == storageKey);
                        if (provider == null)
                        {
                            LogError($"Tried to load activated template with storage key {storageKey}, but no template with that storage key was found among the available templates. Might belong to a mod that was removed");
                            template.LoadFailed = true;
                            continue;
                        }
                        template.LoadFailed = true;
                        Invoking.Safe(() =>
                        {
                            TryActivateProvider(template.PolicyName, provider.Create(template.Settings), deactivateExisting: false);
                            template.LoadFailed = false;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Tools relating to setting up the indexing in <see cref="Toolkit"/>.
        /// </summary>
        public static class Indexing
        {
            /// <summary>
            /// Manages the table that contains <see cref="Verse.ThingFilter"/>.
            /// </summary>
            public static class ThingFilter
            {
                /// <summary>
                /// The name of the root table that contains all defs in the game.
                /// </summary>
                public const string TableName = nameof(Verse.ThingFilter);

                /// <summary>
                /// Configures the schema to include the table for defs.
                /// </summary>
                public static void EnsureTable()
                {
                    Toolkit.Indexing.ConfigureSchema += ConfigureSchema;
                }
                /// <summary>
                /// Configures the snapshot orchestrator to include the gatherer for defs, which is responsible for collecting all defs in the game and pushing them to the snapshot manager.
                /// </summary>
                public static void EnsureGatherer()
                {
                    Toolkit.Indexing.ConfigureOrchestrator += ConfigureGathering;
                }
                /// <summary>
                /// Returns the latest snapshot of the table containing all thing filters in the game.
                /// </summary>
                /// <returns>The latest snapshot of the table containing all thing filters in the game, or null if the table is not available.</returns>
                public static IReadOnlyTable<Verse.ThingFilter> GetTable()
                {
                    return Toolkit.Indexing.Manager.DatabaseSnapshot?.GetTable<Verse.ThingFilter>(TableName);
                }
                /// <summary>
                /// Returns the current table containing all thing filters in the game. This is the live table that is being updated by the gatherer and is used for indexing and searching thing filters.
                /// </summary>
                /// <returns>The current table containing all thing filters in the game, or null if the table is not available.</returns>
                public static IReadOnlyTable<Verse.ThingFilter> GetCurrentTable()
                {
                    return Toolkit.Indexing.Manager.Database?.GetTable<Verse.ThingFilter>(TableName);
                }
                /// <summary>
                /// Adds addition configuration for the table.
                /// </summary>
                /// <param name="builder">The table builder to configure.</param>
                public static void ConfigureTable(Action<ITableBuilder<Verse.ThingFilter>> builder)
                {
                    builder = Helpers.Guard.NotNull(builder, nameof(builder));
                    EnsureTable();

                    Toolkit.Indexing.ConfigureSchema += b => b.WithTable<Verse.ThingFilter>(TableName, builder);
                }
                private static void ConfigureSchema(IDatabaseSchemaBuilder builder)
                {
                    builder.WithTable<Verse.ThingFilter>(TableName);
                }
                private static void ConfigureGathering(ISnapshotOrchestratorBuilder builder)
                {
                    builder.With(ThingFilterGatherer.Instance);
                }
            }
        }

        /// <summary>
        /// Contains the settings for <see cref="DynamicFiltersToolkit"/>.
        /// </summary>
        public class DynamicFiltersToolkitSettings : ModSettings
        {
            /// <summary>
            /// Allows policies to be defines storages and allow lists.
            /// </summary>

            public bool EnableStorageFiltering = true;
            /// <summary>
            /// Enables some readonly policies with common use cases.
            /// </summary>
            public bool EnablePresets = false;
            /// <summary>
            /// The list of activated templates. This can be used to restore the activated policies when the game is loaded, or to display the activated policies in the UI.
            /// </summary>
            public List<ActivatedTemplates> ActiveTemplates = new List<ActivatedTemplates>();

            /// <inheritdoc/>
            override public void ExposeData()
            {
                base.ExposeData();
                Scribe_Collections.Look(ref ActiveTemplates, nameof(ActiveTemplates), LookMode.Deep);
                Scribe_Values.Look(ref EnablePresets, nameof(EnablePresets), defaultValue: false);
                Scribe_Values.Look(ref EnableStorageFiltering, nameof(EnableStorageFiltering), defaultValue: true);

                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    Toolkit.Hooks.Manager.Trigger(new Changed(this));
                }
            }

            /// <summary>
            /// Contains the user settings for a policy created by a template.
            /// </summary>
            public class ActivatedTemplates : IExposable
            {
                /// <summary>
                /// The key that references the template that created the activated policies.
                /// </summary>
                public string StorageKey;
                /// <summary>
                /// The unique policy name that was selected by the user.
                /// </summary>
                public string PolicyName;
                /// <summary>
                /// The user settings for the activated policies.
                /// </summary>
                public IExposable Settings;
                internal bool LoadFailed;

                /// <inheritdoc/>
                public void ExposeData()
                {
                    Scribe_Values.Look(ref StorageKey, nameof(StorageKey));
                    Scribe_Values.Look(ref PolicyName, nameof(PolicyName));
                    Scribe_Deep.Look(ref Settings, nameof(Settings));
                }
            }

            /// <summary>
            /// Raised when the settings are saved/loaded.
            /// </summary>
            public class Changed
            {
                /// <summary>
                /// The settings that were changed.
                /// </summary>
                public DynamicFiltersToolkitSettings Settings { get; }

                /// <inheritdoc cref="Changed"/>
                /// <param name="settings"><inheritdoc cref="Settings"/></param>
                public Changed(DynamicFiltersToolkitSettings settings)
                {
                    Settings = Guard.NotNull(settings, nameof(settings));
                }
            }
        }
    }
}
