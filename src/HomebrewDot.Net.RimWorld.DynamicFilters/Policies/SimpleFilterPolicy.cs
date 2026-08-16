using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Policies.Components;
using HomebrewDot.Net.Rimworld.Policies.Templates;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.State;
using HomebrewDot.Net.Rimworld.UI;
using HomebrewDot.Net.Rimworld.UI.Components;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Policies
{
    /// <summary>
    /// A simple filter policy that can be configured with a list of conditions on the properties of the filtered objects. This is a very flexible policy that can be used to create a wide variety of filters based on the properties of the filtered objects. The conditions are defined as a list of <see cref="SimpleFilterPolicyCondition"/>s, which specify the property to check, the operator to use for the comparison, and the value to compare to. The policy will include all objects that satisfy all the conditions (combined with "And" or "Or" depending on the settings of each condition).
    /// </summary>
    public class SimpleFilterPolicy : IDynamicPolicyTemplate
    {
        private const float BottomButtonsHeight = 34f;

        private Vector2 _conditionScroll = Vector2.zero;
        private Vector2 _descriptionScroll = Vector2.zero;

        /// <summary>
        /// The singleton instance of the <see cref="SimpleFilterPolicy"/> template.
        /// </summary>
        public static SimpleFilterPolicy Instance { get; } = new SimpleFilterPolicy();

        private SimpleFilterPolicy()
        {

        }

        /// <inheritdoc/>
        public string StorageKey => $"{DynamicFiltersToolkit.ModId}.{typeof(SimpleFilterPolicy).Name}";
        /// <inheritdoc/>
        public bool Singleton => false;

        /// <inheritdoc/>
        public IEnumerable<string> ValidateSettings(IExposable settings)
        {
            if(settings is not SimpleFilterPolicySettings typedSettings)
            {
                yield return "Unexpected settings type.";
                yield break;
            }

            if (typedSettings?.Conditions is null || !typedSettings.Conditions.Any())
            {
                yield return "At least 1 condition should be defined";
                yield break;
            }
            for(int i = 0; i < typedSettings.Conditions.Count; i++)
            {
                var condition = typedSettings.Conditions[i];
                foreach(var conditionError in ValidateCondition(condition))
                {
                    yield return $"Condition {i}: {conditionError}";
                }
            }
        }
        private IEnumerable<string> ValidateCondition(SimpleFilterPolicyCondition condition)
        {
            // Group-only conditions (from _staticDef with Conditions but no With) don't need leaf validation
            var def = condition.Condition;
            var isGroupOnly = def != null && def.Conditions?.Length > 0 && def.With == null;

            if (!isGroupOnly)
            {
                if (string.IsNullOrWhiteSpace(condition.Config.CompareDefault) && string.IsNullOrWhiteSpace(condition.Config.CompareType))
                {
                    yield return "Property path cannot be empty.";
                }
                else if (!string.IsNullOrWhiteSpace(condition.Config.CompareDefault) && !System.Text.RegularExpressions.Regex.IsMatch(condition.Config.CompareDefault, DynamicFiltersToolkitConstants.Policy.PropertyPathRegex))
                {
                    yield return $"Invalid property path: {condition.Config.CompareDefault}. Should match regex: {DynamicFiltersToolkitConstants.Policy.PropertyPathRegex}";
                }

                var operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
                if (string.IsNullOrWhiteSpace(condition.Config.Operator))
                {
                    yield return "Operator cannot be empty.";
                }
                else if (!operatorTypes.ContainsKey(condition.Config.Operator))
                {
                    yield return $"Unknown operator: {condition.Config.Operator}. No operator type registered with this name.";
                }
            }
        }
        /// <inheritdoc/>
        public IDynamicPolicyProvider Create(IExposable settings)
        {
            if(settings is not SimpleFilterPolicySettings typedSettings)
            {
                throw new ArgumentException($"Invalid settings type. Expected {typeof(SimpleFilterPolicySettings).FullName}", nameof(settings));
            }

            return new Provider(this, typedSettings);
        }
        /// <inheritdoc/>
        public void DrawSettings(Rect rect, ref IExposable settings)
        {
            if (settings is not SimpleFilterPolicySettings typedSettings)
            {
                typedSettings = new SimpleFilterPolicySettings();
                settings = typedSettings;
            }

            var cursorY = rect.y;

            var thingDefRect = new Rect(rect.x, cursorY, rect.width, 24f);
            Widgets.CheckboxLabeled(thingDefRect, "ForThingDef", ref typedSettings.ThingDef);
            cursorY = thingDefRect.yMax + 6f;

            if (!typedSettings.ThingDef)
            {
                var lazyEvaluationRect = new Rect(rect.x, cursorY, rect.width, 24f);
                Widgets.CheckboxLabeled(lazyEvaluationRect, "Lazy Evaluation", ref typedSettings.LazyEvaluation);
                cursorY = lazyEvaluationRect.yMax + 6f;

                if (!typedSettings.LazyEvaluation)
                {
                    var requireMapContextRect = new Rect(rect.x, cursorY, rect.width, 24f);
                    Widgets.CheckboxLabeled(requireMapContextRect, "Require Map Context", ref typedSettings.RequireMapContext);
                    cursorY = requireMapContextRect.yMax + 6f;
                }
            }

            var listLabelRect = new Rect(rect.x, cursorY, rect.width, 22f);
            Widgets.Label(listLabelRect, "Conditions");
            cursorY = listLabelRect.yMax + 4f;

            var remaining = rect.yMax - BottomButtonsHeight - 8f - cursorY;
            var minListHeight = Mathf.Floor(rect.height * 0.5f);
            var listHeight = Mathf.Max(minListHeight, remaining);

            var listOutRect = new Rect(rect.x, cursorY, rect.width, listHeight);
            Widgets.DrawMenuSection(listOutRect);
            DrawConditionsList(listOutRect.ContractedBy(6f), typedSettings);

            var addRect = new Rect(rect.x, listOutRect.yMax + 6f, 170f, BottomButtonsHeight);
            RuleListUi.DrawActionButton(addRect, "Add Condition", () =>
            {
                EditorWindowStack.OpenNested(new ConditionDefEditorWindow(null, config =>
                {
                    typedSettings.Conditions.Add(SimpleFilterPolicyCondition.FromConfig(config));
                }));
            });
        }

        private void DrawConditionsList(Rect outRect, SimpleFilterPolicySettings settings)
        {
            var conditions = settings.Conditions ?? (settings.Conditions = new List<SimpleFilterPolicyCondition>());

            RuleListUi.Draw(
                outRect,
                ref _conditionScroll,
                conditions,
                "- No conditions defined",
                BuildConditionSummary,
                editIndex =>
                {
                    var editingCondition = conditions[editIndex];
                    var editingConfig = editingCondition.IsStatic
                        ? ConditionDefConfig.FromConditionDef(editingCondition.Condition)
                        : editingCondition.Config;
                    EditorWindowStack.OpenNested(new ConditionDefEditorWindow(
                        editingConfig,
                        config =>
                        {
                            conditions[editIndex] = SimpleFilterPolicyCondition.FromConfig(config);
                        }));
                },
                condition => condition.Copy(),
                condition => condition.IsOr,
                (condition, isOr) => condition.IsOr = isOr);
        }

        private static string BuildConditionSummary(SimpleFilterPolicyCondition condition)
        {
            if (condition == null)
            {
                return "(null condition)";
            }

            return condition.Condition?.ToCompactString() ?? "(null condition)";
        }

        /// <inheritdoc/>
        public string GetShortDescription() => "Filter for matching thing(defs) based on specified conditions on their properties.";
        /// <inheritdoc/>
        public string GetLongDescription(IExposable settings)
        {
            if(settings is null) return GetShortDescription();
            if(settings is not SimpleFilterPolicySettings typedSettings) return GetShortDescription();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Match all").Append(typedSettings.ThingDef ? " ThingDefs " : " Things ")
                .Append("that satisfy the following conditions:").AppendLine();
            if(!typedSettings.Conditions.Any())
            {
                stringBuilder.Append("No conditions defined. This filter will match no ").Append(typedSettings.ThingDef ? "ThingDefs." : "Things.");
            }
            else
            {
                _ = ConditionDef.GroupToString(typedSettings.Conditions.Select(c => c.Condition).ToArray(), stringBuilder, true);

            }
            return stringBuilder.ToString();
        }
        /// <inheritdoc/>
        public string GetTitle() => "Simple Filter Policy";

        private class Provider : IDynamicPolicyProvider
        {
            // Fields
            private readonly SimpleFilterPolicy _parent;
            private readonly SimpleFilterPolicySettings _settings;

            public Provider(SimpleFilterPolicy parent, SimpleFilterPolicySettings settings)
            {
                _parent = Guard.NotNull(parent, nameof(parent));
                _settings = Guard.NotNull(settings, nameof(settings));
            }

            /// <inheritdoc/>
            public void Activate(string name, IDynamicPolicyProviderActivationContext context)
            {
                Logging.Log($"Activating simple filter policy: {name}");

                if (_settings.ThingDef)
                {
                    Toolkit.Indexing.Def.EnsureGatherer();
                    Toolkit.Indexing.Def.Thing.EnsureTable();
                }
                else
                {
                    Toolkit.Indexing.Thing.EnsureGatherer();
                    Toolkit.Indexing.Thing.EnsureTable();
                }

                context.WithLabel("Simple Filter")
                        .WithTitle(_parent.GetTitle())
                        .WithDescription(_parent.GetLongDescription(_settings));

                var isLazy = !_settings.ThingDef && _settings.LazyEvaluation;

                if (!isLazy)
                {
                    Toolkit.Collecting.Rebuild(name, x =>
                    {
                        foreach (var condition in _settings.Conditions)
                        {
                            var def = condition.Condition;
                            _ = x.CompareFrom(def);
                        }
                        return _settings.ThingDef ? 
                        x.CollectFromSnapshot(d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName), d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName).GetSnapshot(), false) : 
                        x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).GetSnapshot());
                    });
                    if (_settings.ThingDef)
                    {
                        context.AvailableFor<Map, ThingDef>(new CollectionPolicy(name, false));
                    }
                    else
                    {
                        context.AvailableFor<Map, Thing>(new CollectionPolicy(name, _settings.RequireMapContext));
                    }
                }
                else
                {
                    var collection = Toolkit.Collecting.Build(name, x =>
                    {
                        foreach (var condition in _settings.Conditions)
                        {
                            var def = condition.Condition;
                            _ = x.CompareFrom(def);
                        }
                        return x;
                    });
                    var collections = Toolkit.Collecting.GetAllDefinitions();
                    var comparer = Toolkit.Collecting.Comparator;
                    context.AvailableFor<Map, Thing>(new LazyCollectionPolicy(name, collection, comparer, collections, (Toolkit.Indexing.Manager.Database as IDatabase)?.AsTyped<Thing>()));
                }
            }
            /// <inheritdoc/>
            public void Deactivate(Action disposePolicies)
            {
            }
        }
    }
    /// <summary>
    /// Contains the settings for a <see cref="SimpleFilterPolicy"/>.
    /// </summary>
    public class SimpleFilterPolicySettings : BaseCollectionFilterPolicySettings, IExposable
    {
        /// <summary>
        /// The conditions for the filter policy. This is a list of <see cref="SimpleFilterPolicyCondition"/>s that define the conditions for the filter policy.
        /// </summary>
        public List<SimpleFilterPolicyCondition> Conditions = new List<SimpleFilterPolicyCondition>();

        /// <inheritdoc/>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref Conditions, "Conditions", LookMode.Deep);
        }
    }

    /// <summary>
    /// A condition for a <see cref="SimpleFilterPolicy"/>. Backed by a <see cref="ConditionDefConfig"/> that holds all editable state.
    /// </summary>
    public class SimpleFilterPolicyCondition : IExposable
    {
        // Fields
        private readonly ConditionDef _staticDef;

        public SimpleFilterPolicyCondition()
        {
            
        }

        private SimpleFilterPolicyCondition(ConditionDef staticDef)
        {
            _staticDef = Guard.NotNull(staticDef, nameof(staticDef));
        }

        /// <summary>
        /// Indicates whether this condition is backed by a static <see cref="ConditionDef"/> rather than an
        /// editable <see cref="ConditionDefConfig"/>.
        /// </summary>
        public bool IsStatic => _staticDef != null;

        /// <summary>
        /// The configuration backing this condition.
        /// </summary>
        public ConditionDefConfig Config = new ConditionDefConfig();

        /// <summary>
        /// Gets the <see cref="ConditionDef"/> representation of this condition, which can be used in the filtering system to evaluate items against this condition.
        /// </summary>
        public ConditionDef Condition => _staticDef ?? Config.ToConditionDef();

        /// <summary>
        /// If the next condition defined after the current one should be combined with this condition using an "Or" instead of an "And". Default is false (combined with "And").
        /// </summary>
        public bool IsOr
        {
            get => Config.IsOr;
            set => Config.IsOr = value;
        }

        /// <summary>
        /// Inverts the condition, so it matches when the underlying comparison would not match and vice versa. Default is false.
        /// </summary>
        public bool Inverted
        {
            get => Config.Inverted;
            set => Config.Inverted = value;
        }

        /// <summary>
        /// Creates a new condition wrapping the supplied config.
        /// </summary>
        public static SimpleFilterPolicyCondition FromConfig(ConditionDefConfig config)
            => new SimpleFilterPolicyCondition { Config = config ?? new ConditionDefConfig() };

        /// <summary>
        /// Creates an independent copy of this condition. Static conditions keep their backing
        /// <see cref="ConditionDef"/>; config-backed conditions copy their <see cref="ConditionDefConfig"/>.
        /// </summary>
        /// <returns>A new <see cref="SimpleFilterPolicyCondition"/> with the same state.</returns>
        public SimpleFilterPolicyCondition Copy()
        {
            if (IsStatic)
            {
                return FromDef(_staticDef);
            }

            return FromConfig(Config == null ? new ConditionDefConfig() : new ConditionDefConfig(Config));
        }

        /// <summary>
        /// Creates a new condition based on the supplied <see cref="ConditionDef"/>.
        /// </summary>
        /// <param name="def">The condition definition to base the new condition on.</param>
        /// <returns>A new <see cref="SimpleFilterPolicyCondition"/> based on the supplied definition.</returns>
        public static SimpleFilterPolicyCondition FromDef(ConditionDef def)
            => new SimpleFilterPolicyCondition(def);

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Deep.Look(ref Config, "Config");
            if (Config == null) Config = new ConditionDefConfig();
        }
    }
}

