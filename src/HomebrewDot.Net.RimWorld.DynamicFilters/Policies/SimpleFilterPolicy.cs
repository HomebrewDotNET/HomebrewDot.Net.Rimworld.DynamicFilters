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
        private const float RowHeight = 28f;
        private const float RowGap = 4f;
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
            var disallowRect = new Rect(rect.x, cursorY, rect.width, 24f);
            Widgets.CheckboxLabeled(disallowRect, "Disallow Matching", ref typedSettings.DisallowMatching);
            cursorY = disallowRect.yMax + 6f;

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
            DrawActionButton(addRect, "Add Condition", () =>
            {
                Find.WindowStack.Add(new ConditionDefEditorWindow(null, config =>
                {
                    typedSettings.Conditions.Add(SimpleFilterPolicyCondition.FromConfig(config));
                }));
            });
        }

        private void DrawConditionsList(Rect outRect, SimpleFilterPolicySettings settings)
        {
            var conditions = settings.Conditions ?? (settings.Conditions = new List<SimpleFilterPolicyCondition>());
            var viewHeight = Mathf.Max(outRect.height, conditions.Count * (RowHeight + RowGap) + 4f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref _conditionScroll, viewRect);

            if (conditions.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 22f), "- No conditions defined");
                Widgets.EndScrollView();
                return;
            }

            var y = 0f;
            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                var editRect = new Rect(rowRect.xMax - 92f, rowRect.y, 28f, RowHeight);
                var deleteRect = new Rect(rowRect.xMax - 60f, rowRect.y, 28f, RowHeight);
                var logicRect = new Rect(rowRect.xMax - 150f, rowRect.y, 54f, RowHeight);
                var textWidth = rowRect.width - (i < conditions.Count - 1 ? 154f : 94f) - 8f;
                var textRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, textWidth, RowHeight - 8f);

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Widgets.DrawMenuSection(rowRect);
                Widgets.Label(textRect, BuildConditionSummary(condition));

                if (i < conditions.Count - 1)
                {
                    var logicLabel = condition.IsOr ? "OR" : "AND";
                    DrawActionButton(logicRect, logicLabel, () => condition.IsOr = !condition.IsOr);
                }

                DrawActionButton(editRect, "E", () =>
                {
                    var editIndex = i;
                    var editingConfig = conditions[editIndex].Config;
                    Find.WindowStack.Add(new ConditionDefEditorWindow(
                        editingConfig,
                        config => { }));
                });

                DrawActionButton(deleteRect, "X", () =>
                {
                    conditions.RemoveAt(i);
                });

                y += RowHeight + RowGap;
            }

            Widgets.EndScrollView();
        }

        private static string BuildConditionSummary(SimpleFilterPolicyCondition condition)
        {
            if (condition == null)
            {
                return "(null condition)";
            }

            return condition.Condition.ToString();
        }

        private static void DrawActionButton(Rect rect, string label, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
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

                Toolkit.Collecting.Build(name, x =>
                {
                    foreach (var condition in _settings.Conditions)
                    {
                        var def = condition.Condition;
                        _ = x.CompareFrom(def);
                    }
                    return _settings.ThingDef ? x.CollectFromSnapshot(d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName), d =>  d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName).Enumerate<IIndexed<ThingDef>>(), false) : x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).Enumerate<IIndexed<Thing>>());
                });

                context.WithLabel("Simple Filter")
                       .WithDescription(_parent.GetLongDescription(_settings));

                if (_settings.ThingDef)
                {
                    context.AvailableFor<Map, ThingDef>(new Policy(name));
                }
                else
                {
                    context.AvailableFor<Map, Thing>(new Policy(name));
                }
            }
            /// <inheritdoc/>
            public void Deactivate(Action disposePolicies)
            {
            }
        }

        private class Policy : IDynamicPolicy<Map, ThingDef>, IDynamicPolicy<Map, Thing>, IDisposable
        {
            // Fields
            private readonly string _name;

            // State
            internal int _filterTracker;

            // Properties
            /// <inheritdoc/>
            string IDynamicPolicy<Map, Thing>.Name => _name;
            /// <inheritdoc/>
            string IDynamicPolicy<Map, ThingDef>.Name => _name;

            public Policy(string name)
            {
                _name = Guard.NotNullOrWhitespace(name, nameof(name));
            }
            /// <inheritdoc/>
            IDynamicFilter<Map, Thing> IDynamicPolicy<Map, Thing>.GetFilter(Map scope)
            {
                scope = Guard.NotNull(scope, nameof(scope));

                var mapCollectionName = $"{scope.GetUniqueLoadID()}.{_name}";
                Toolkit.Collecting.Build(mapCollectionName, x => x.Compare.Indexed(nameof(Map))
                                                                  .With.Equal(scope)
                                                                  .CollectFromCollection<ICollectionBuilder, Thing>(_name)
                                        );
                return new Filter<Thing>(mapCollectionName, scope, this);
            }
            /// <inheritdoc/>
            IDynamicFilter<Map, ThingDef> IDynamicPolicy<Map, ThingDef>.GetFilter(Map scope)
            {
                scope = Guard.NotNull(scope, nameof(scope));

                // Defs are not really scoped per map so no need for extra filtering
                _filterTracker++;
                return new Filter<ThingDef>(_name, scope, this);
            }
            /// <inheritdoc/>
            public void Dispose()
            {
                if(_filterTracker <= 0)
                    Toolkit.Collecting.Remove(_name);
            }
        }

        private class Filter<T> : IDynamicFilter<Map, T>, IDisposable where T : class
        {
            // Fields
            private readonly string _collectionName;
            private readonly Policy _policy;

            // State
            private ICollector<T> _collection;
            private int _lastCollectionVersion = -1;

            // Properties
            /// <inheritdoc/>
            public Map Scope { get; }
            /// <inheritdoc/>
            public IDynamicPolicy<Map, T> Policy => (IDynamicPolicy<Map,T>)_policy;

            public Filter(string collectionName, Map scope, Policy policy)
            {
                _collectionName = Guard.NotNullOrWhitespace(collectionName, nameof(collectionName));
                Scope = Guard.NotNull(scope, nameof(scope));
                _policy = Guard.NotNull(policy, nameof(policy));
                if(Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    _collection = typedCollector;
                }
            }
            /// <inheritdoc/>
            public bool Update(IStateStore<Map> stateStore)
            {
                bool isNew = false;
                if (Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    isNew = _collection != typedCollector;
                    _collection = typedCollector;
                }
                if(_collection is SnapshotCollector<T> snapshotCollector)
                {
                    if (snapshotCollector.Version != _lastCollectionVersion)
                    {
                        _lastCollectionVersion = snapshotCollector.Version;
                        return true;
                    }
                }
                else
                {
                    // If it's not a snapshot collector we assume it's always updated since we don't have versioning for other collector types
                    return true;
                }
                return isNew;
            }
            /// <inheritdoc/>
            bool IDynamicFilter<Map, T>.Filter(T item)
            {
                var collection = _collection;
                if(collection is not null)
                {
                    return collection.Contains(item);
                }
                return false;
            }
            /// <inheritdoc/>
            public void Dispose()
            {
                if(typeof(ThingDef) == typeof(T))
                {
                    _policy._filterTracker--;
                }
                else
                {
                    Toolkit.Collecting.Remove(_collectionName);
                }
            }
        }
    }
    /// <summary>
    /// Contains the settings for a <see cref="SimpleFilterPolicy"/>.
    /// </summary>
    public class SimpleFilterPolicySettings : IExposable
    {
        /// <summary>
        /// Filter applies to <see cref="Verse.ThingDef"/>s. Default is true.
        /// When false it applies to <see cref="Verse.Thing"/>.
        /// </summary>
        public bool ThingDef = true;
        /// <summary>
        /// Inverts the policy to filter out matching defs/things instead of including them. 
        /// When false it will include matching and exclude the rest, when true it will exclude matching and include the rest. Default is false (include matching).
        /// </summary>
        public bool DisallowMatching = false;

        /// <summary>
        /// The conditions for the filter policy. This is a list of <see cref="SimpleFilterPolicyCondition"/>s that define the conditions for the filter policy.
        /// </summary>
        public List<SimpleFilterPolicyCondition> Conditions = new List<SimpleFilterPolicyCondition>();

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingDef, "ThingDef");
            Scribe_Values.Look(ref DisallowMatching, "DisallowMatching");
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
        /// Creates a new condition wrapping the supplied config.
        /// </summary>
        public static SimpleFilterPolicyCondition FromConfig(ConditionDefConfig config)
            => new SimpleFilterPolicyCondition { Config = config ?? new ConditionDefConfig() };

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

