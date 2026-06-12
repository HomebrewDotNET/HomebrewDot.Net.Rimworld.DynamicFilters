using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.UI.Components;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.State;
using HomebrewDot.Net.Rimworld.Collecting;

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
            if (string.IsNullOrWhiteSpace(condition.PropertyPath))
            {
                yield return "Property path cannot be empty.";
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(condition.PropertyPath, DynamicFiltersToolkitConstants.Policy.PropertyPathRegex))
            {
                yield return $"Invalid property path: {condition.PropertyPath}. Should match regex: {DynamicFiltersToolkitConstants.Policy.PropertyPathRegex}";
            }

            var operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
            if (string.IsNullOrWhiteSpace(condition.Operator))
            {
                yield return "Operator cannot be empty.";
            }
            else if (!operatorTypes.ContainsKey(condition.Operator))
            {
                yield return $"Unknown operator: {condition.Operator}. No operator type registered with this name.";
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

            var listLabelRect = new Rect(rect.x, cursorY, rect.width, 22f);
            Widgets.Label(listLabelRect, "Conditions");
            cursorY = listLabelRect.yMax + 4f;

            var listRect = new Rect(rect.x, cursorY, rect.width, Mathf.Max(80f, rect.height - (cursorY - rect.y) - BottomButtonsHeight - 8f));
            Widgets.DrawMenuSection(listRect);
            DrawConditionsList(listRect.ContractedBy(6f), typedSettings);

            var addRect = new Rect(rect.x, listRect.yMax + 6f, 170f, BottomButtonsHeight);
            DrawActionButton(addRect, "Add Condition", () =>
            {
                Find.WindowStack.Add(new ConditionEditorWindow(null, condition =>
                {
                    typedSettings.Conditions.Add(condition);
                }));
            });
        }

        private static void DrawConditionsList(Rect outRect, SimpleFilterPolicySettings settings)
        {
            var conditions = settings.Conditions ?? (settings.Conditions = new List<SimpleFilterPolicyCondition>());
            var viewHeight = Mathf.Max(outRect.height, conditions.Count * (RowHeight + RowGap) + 4f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref settings.ConditionScroll, viewRect);

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
                    Find.WindowStack.Add(new ConditionEditorWindow(CloneCondition(condition), updated =>
                    {
                        updated.IsOr = conditions[editIndex].IsOr;
                        conditions[editIndex] = updated;
                    }));
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

            return $"Compare {{{condition.PropertyPath}}} With {{{condition.Operator}}} To {{{condition.Value?.ToString() ?? "null"}}}";
        }

        private static SimpleFilterPolicyCondition CloneCondition(SimpleFilterPolicyCondition source)
        {
            if (source == null)
            {
                return new SimpleFilterPolicyCondition();
            }

            return new SimpleFilterPolicyCondition
            {
                PropertyPath = source.PropertyPath,
                Operator = source.Operator,
                ValueType = source.ValueType,
                TextValue = source.TextValue,
                NumberValue = source.NumberValue,
                DecimalValue = source.DecimalValue,
                IsOr = source.IsOr,
            };
        }

        private static void DrawActionButton(Rect rect, string label, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            Widgets.Label(rect.ContractedBy(4f), label);
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
                for (int i = 0; i < typedSettings.Conditions.Count; i++)
                {
                    var condition = typedSettings.Conditions[i];
                    stringBuilder.Append("- ").Append("Compare {").Append(condition.PropertyPath).Append("} With {")
                        .Append(condition.Operator).Append("} To {").Append(condition.Value?.ToString() ?? "null").Append("}");
                    if (i < typedSettings.Conditions.Count - 1)
                    {
                        stringBuilder.Append(condition.IsOr ? " OR" : " AND");
                    }
                    stringBuilder.AppendLine();
                }
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
                        _ = x.Compare.Indexed(condition.PropertyPath)
                             .With.Operator(condition.Operator)
                             .To.Value(condition.Value)
                             .AndOr(!condition.IsOr);
                    }
                    return _settings.ThingDef ? x.CollectFromSnapshot(d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName).Version, d =>  d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName).Enumerate<IIndexed<ThingDef>>()) : x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).Version, d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).Enumerate<IIndexed<Thing>>());
                });

                context.WithLabel("Simple Filter")
                       .WithDescription(_parent.GetLongDescription(_settings));

                Toolkit.Indexing.ReloadOrchestration();

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
                return new Filter<ThingDef>(_name, scope, this);
            }
            /// <inheritdoc/>
            public void Dispose()
            {
                Toolkit.Collecting.Remove(_name);
            }
        }

        private class Filter<T> : IDynamicFilter<Map, T>, IDisposable where T : class
        {
            // Fields
            private readonly string _collectionName;

            // State
            private ICollector<T> _collection;

            // Properties
            /// <inheritdoc/>
            public Map Scope { get; }
            /// <inheritdoc/>
            public IDynamicPolicy<Map, T> Policy { get; }

            public Filter(string collectionName, Map scope, IDynamicPolicy<Map, T> policy)
            {
                _collectionName = Guard.NotNullOrWhitespace(collectionName, nameof(collectionName));
                Scope = Guard.NotNull(scope, nameof(scope));
                Policy = Guard.NotNull(policy, nameof(policy));
                if(Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    _collection = typedCollector;
                }
            }
            /// <inheritdoc/>
            public void Update(IStateStore<Map> stateStore)
            {
                if (Toolkit.Collecting.GetAllCollectors().TryGetValue(_collectionName, out var collector) && collector is ICollector<T> typedCollector)
                {
                    _collection = typedCollector;
                }
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
                Toolkit.Collecting.Remove(_collectionName);
            }
        }
    }

    internal sealed class ConditionEditorWindow : Window
    {


        private readonly Action<SimpleFilterPolicyCondition> _onSave;
        private string _propertyPath;
        private string _operator;
        private string _valueText;
        private int _valueNumber;
        private double _valueDecimal;
        private string _valueNumberBuffer;
        private string _valueDecimalBuffer;
        private bool _isOr;
        private ValueInputType _valueMode;
        private string _error;

        public ConditionEditorWindow(SimpleFilterPolicyCondition source, Action<SimpleFilterPolicyCondition> onSave)
        {
            source = source ?? new SimpleFilterPolicyCondition();
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

            _propertyPath = source.PropertyPath ?? string.Empty;
            _operator = source.Operator ?? string.Empty;
            _isOr = source.IsOr;
            _valueMode = source.ValueType;
            _valueText = source.TextValue ?? string.Empty;
            _valueNumber = source.NumberValue;
            _valueDecimal = source.DecimalValue;
            _valueNumberBuffer = _valueNumber.ToString(CultureInfo.InvariantCulture);
            _valueDecimalBuffer = _valueDecimal.ToString(CultureInfo.InvariantCulture);

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
        }

        public override Vector2 InitialSize => new Vector2(760f, 300f);

        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 24f), "Edit Condition");
            cursorY += 30f;

            Widgets.Label(new Rect(inRect.x, cursorY, 90f, 24f), "Compare");
            _propertyPath = Widgets.TextField(new Rect(inRect.x + 96f, cursorY - 2f, inRect.width - 96f, 28f), _propertyPath);
            cursorY += 34f;

            Widgets.Label(new Rect(inRect.x, cursorY, 90f, 24f), "With");
            var operatorRect = new Rect(inRect.x + 96f, cursorY - 2f, inRect.width - 134f, 28f);
            var operatorPickRect = new Rect(operatorRect.xMax + 4f, operatorRect.y, 34f, 28f);
            _operator = Widgets.TextField(operatorRect, _operator);
            DrawActionButton(operatorPickRect, "...", OpenOperatorPicker);
            cursorY += 34f;

            Widgets.Label(new Rect(inRect.x, cursorY, 90f, 24f), "To");
            var valueRect = new Rect(inRect.x + 96f, cursorY - 2f, inRect.width - 224f, 28f);
            var textModeRect = new Rect(valueRect.xMax + 4f, valueRect.y, 34f, 28f);
            var numberModeRect = new Rect(textModeRect.xMax + 4f, valueRect.y, 34f, 28f);
            var decimalModeRect = new Rect(numberModeRect.xMax + 4f, valueRect.y, 34f, 28f);

            if (_valueMode == ValueInputType.Number)
            {
                Widgets.TextFieldNumeric(valueRect, ref _valueNumber, ref _valueNumberBuffer);
            }
            else if (_valueMode == ValueInputType.Decimal)
            {
                Widgets.TextFieldNumeric(valueRect, ref _valueDecimal, ref _valueDecimalBuffer);
            }
            else
            {
                _valueText = Widgets.TextField(valueRect, _valueText);
            }
            DrawModeButton(textModeRect, "T", ValueInputType.Text);
            DrawModeButton(numberModeRect, "N", ValueInputType.Number);
            DrawModeButton(decimalModeRect, "D", ValueInputType.Decimal);
            cursorY += 34f;

            Widgets.CheckboxLabeled(new Rect(inRect.x, cursorY, inRect.width, 24f), "Combine with next using OR", ref _isOr);
            cursorY += 30f;

            if (!string.IsNullOrWhiteSpace(_error))
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 24f), _error);
                GUI.color = Color.white;
            }

            var buttonWidth = 120f;
            var cancelRect = new Rect(inRect.x, inRect.yMax - 36f, buttonWidth, 32f);
            var saveRect = new Rect(cancelRect.xMax + 8f, cancelRect.y, buttonWidth, 32f);

            DrawActionButton(cancelRect, "Cancel", () => Close());
            DrawActionButton(saveRect, "Save", Save);
        }

        private void DrawModeButton(Rect rect, string label, ValueInputType mode)
        {
            if (_valueMode == mode)
            {
                Widgets.DrawHighlightSelected(rect);
            }

            DrawActionButton(rect, label, () => _valueMode = mode);
        }

        private void OpenOperatorPicker()
        {
            var operators = Toolkit.Services
                .GetAllNamed<IOperatorType>()
                .Keys
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var optionsGrid = new Grid<string>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value),
                getTooltip: x => x,
                cellWidth: 300f,
                cellHeight: 28f,
                cellGap: 4f);

            var selectedGrid = new Grid<string>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value),
                getTooltip: x => x,
                cellWidth: 300f,
                cellHeight: 28f,
                cellGap: 4f);

            var initialSelection = string.IsNullOrWhiteSpace(_operator)
                ? null
                : new List<string> { _operator };

            Find.WindowStack.Add(new SelectionWindow<string>(
                title: "Select Operator",
                options: operators,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    if (selected != null && selected.Count > 0)
                    {
                        _operator = selected[0] ?? string.Empty;
                    }
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                initialSelection: initialSelection));
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_propertyPath))
            {
                _error = "Property path cannot be empty.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_operator))
            {
                _error = "Operator cannot be empty.";
                return;
            }

            _onSave(new SimpleFilterPolicyCondition
            {
                PropertyPath = _propertyPath?.Trim() ?? string.Empty,
                Operator = _operator?.Trim() ?? string.Empty,
                ValueType = _valueMode,
                TextValue = _valueText,
                NumberValue = _valueNumber,
                DecimalValue = _valueDecimal,
                IsOr = _isOr,
            });

            Close();
        }

        private static void DrawActionButton(Rect rect, string label, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            Widgets.Label(rect.ContractedBy(4f), label);
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
        /// The conditions for the filter policy. This is a list of <see cref="SimpleFilterPolicyCondition"/>s that define the conditions for the filter policy.
        /// </summary>
        public List<SimpleFilterPolicyCondition> Conditions = new List<SimpleFilterPolicyCondition>();

        /// <summary>
        /// Scroll state for the in-game condition editor UI.
        /// </summary>
        public Vector2 ConditionScroll = Vector2.zero;

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingDef, "ThingDef");
            Scribe_Collections.Look(ref Conditions, "Conditions", LookMode.Deep);
        }
    }

    /// <summary>
    /// A condition for a <see cref="SimpleFilterPolicy"/>. This is a simple class that can be used to define a condition for a filter policy.
    /// </summary>
    public class SimpleFilterPolicyCondition : IExposable
    {
        /// <summary>
        /// Points to the property that should be checked for this condition. Can be a nested property. For example: "def.Label".
        /// </summary>
        public string PropertyPath = string.Empty;
        /// <summary>
        /// The operator to use for this condition.
        /// For example "Equals", "NotEquals", "GreaterThan", "LessThan", etc. The exact operators that are supported will depend on the <see cref="IOperatorType"/>s registered globally.
        /// </summary>
        public string Operator = string.Empty;
        /// <summary>
        /// Defines the type of the value for this condition.
        /// </summary>
        public ValueInputType ValueType = ValueInputType.Text;
        /// <summary>
        /// The text value when the value type is <see cref="ValueInputType.Text"/>.
        /// </summary>
        public string TextValue;
        /// <summary>
        /// The number value when the value type is <see cref="ValueInputType.Number"/>.
        /// </summary>
        public int NumberValue;
        /// <summary>
        /// The decimal value when the value type is <see cref="ValueInputType.Decimal"/>.
        /// </summary>
        public double DecimalValue;
        /// <summary>
        /// The constant value to compare the property to for this condition. The type of this value should be compatible with the type of the property being checked and the operator being used.
        /// </summary>
        public object Value  => ValueType switch
        {
            ValueInputType.Text => TextValue,
            ValueInputType.Number => NumberValue,
            ValueInputType.Decimal => DecimalValue,
            _ => TextValue
        };
        /// <summary>
        /// If the next condition defined after the current one should be combined with this condition using an "Or" instead of an "And". Default is false (combined with "And").
        /// </summary>
        public bool IsOr;

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref PropertyPath, "PropertyPath");
            Scribe_Values.Look(ref Operator, "Operator");
            Scribe_Values.Look(ref ValueType, "ValueType");
            Scribe_Values.Look(ref TextValue, "TextValue");
            Scribe_Values.Look(ref NumberValue, "NumberValue");
            Scribe_Values.Look(ref DecimalValue, "DecimalValue");
            Scribe_Values.Look(ref IsOr, "IsOr");
        }
    }
    /// <summary>
    /// Defines the type of a constant value
    /// </summary>
    public enum ValueInputType
    {
        /// <summary>
        /// Value is a string.
        /// </summary>
        Text,
        /// <summary>
        /// Value is an integer.
        /// </summary>
        Number,
        /// <summary>
        /// Value is a floating point number (double).
        /// </summary>
        Decimal,
    }
}
