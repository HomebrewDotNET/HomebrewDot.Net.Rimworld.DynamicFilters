using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Policies.Components;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.State;
using HomebrewDot.Net.Rimworld.UI.Components;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Policies
{
    /// <summary>
    /// A more complex filter policy compared to the <see cref="SimpleFilterPolicy"/>. In addition to conditions, it allows for inclusion or exclusion of other already existing collections.
    /// </summary>
    public class ComplexFilterPolicy : IDynamicPolicyTemplate
    {
        private const float BottomButtonsHeight = 34f;

        /// <summary>
        /// The singleton instance of the <see cref="ComplexFilterPolicy"/> template.
        /// </summary>
        public static ComplexFilterPolicy Instance { get; } = new ComplexFilterPolicy();

        private ComplexFilterPolicy()
        {

        }

        /// <inheritdoc/>
        public string StorageKey => $"{DynamicFiltersToolkit.ModId}.{typeof(ComplexFilterPolicy).Name}";
        /// <inheritdoc/>
        public bool Singleton => false;

        /// <inheritdoc/>
        public IEnumerable<string> ValidateSettings(IExposable settings)
        {
            if (settings is not ComplexFilterPolicySettings typedSettings)
            {
                yield return "Unexpected settings type.";
                yield break;
            }

            bool hasConditions = typedSettings.Config.Conditions is not null && typedSettings.Config.Conditions.Any();
            bool hasIncludes = typedSettings.Config.Inclusions is not null && typedSettings.Config.Inclusions.Any();
            bool hasExcludes = typedSettings.Config.Exclusions is not null && typedSettings.Config.Exclusions.Any();

            if (!hasConditions && !hasIncludes && !hasExcludes)
            {
                yield return "At least one condition, inclusion, or exclusion must be defined.";
                yield break;
            }

            if (hasConditions)
            {
                for (int i = 0; i < typedSettings.Config.Conditions.Count; i++)
                {
                    var condition = typedSettings.Config.Conditions[i];
                    foreach (var conditionError in ValidateCondition(condition))
                    {
                        yield return $"Condition {i}: {conditionError}";
                    }
                }
            }
            if (hasIncludes)
            {
                for (int i = 0; i < typedSettings.Config.Inclusions.Count; i++)
                {
                    var inclusion = typedSettings.Config.Inclusions[i];
                    foreach (var inclusionError in ValidateCollectionReference(inclusion))
                    {
                        yield return $"Inclusion {i}: {inclusionError}";
                    }
                }
            }
            if (hasExcludes)
            {
                for (int i = 0; i < typedSettings.Config.Exclusions.Count; i++)
                {
                    var exclusion = typedSettings.Config.Exclusions[i];
                    foreach (var exclusionError in ValidateCollectionReference(exclusion))
                    {
                        yield return $"Exclusion {i}: {exclusionError}";
                    }
                }
            }
        }
        private IEnumerable<string> ValidateCondition(ConditionDefConfig condition)
        {
            if (string.IsNullOrWhiteSpace(condition.CompareDefault) && string.IsNullOrWhiteSpace(condition.CompareType))
            {
                yield return "Property path cannot be empty.";
            }
            else if (!string.IsNullOrWhiteSpace(condition.CompareDefault) && !System.Text.RegularExpressions.Regex.IsMatch(condition.CompareDefault, DynamicFiltersToolkitConstants.Policy.PropertyPathRegex))
            {
                yield return $"Invalid property path: {condition.CompareDefault}. Should match regex: {DynamicFiltersToolkitConstants.Policy.PropertyPathRegex}";
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

        private IEnumerable<string> ValidateCollectionReference(CollectionConditionDefConfig collection)
        {
            if (string.IsNullOrWhiteSpace(collection.Name))
            {
                yield return "Collection name cannot be empty.";
            }
            else if (!Toolkit.Collecting.GetAllCollectors().ContainsKey(collection.Name))
            {
                yield return $"Unknown collection: {collection.Name}. No collection registered with this name.";
            }
        }

        /// <inheritdoc/>
        public IDynamicPolicyProvider Create(IExposable settings)
        {
            if (settings is not ComplexFilterPolicySettings typedSettings)
            {
                throw new ArgumentException($"Invalid settings type. Expected {typeof(ComplexFilterPolicySettings).FullName}", nameof(settings));
            }

            return new Provider(this, typedSettings);
        }
        /// <inheritdoc/>
        public void DrawSettings(Rect rect, ref IExposable settings)
        {
            if (settings is not ComplexFilterPolicySettings typedSettings)
            {
                typedSettings = new ComplexFilterPolicySettings();
                settings = typedSettings;
            }

            typedSettings.Config ??= new CollectionDefConfig();

            var cursorY = rect.y;

            var thingDefRect = new Rect(rect.x, cursorY, rect.width, 24f);
            Widgets.CheckboxLabeled(thingDefRect, "ForThingDef", ref typedSettings.ThingDef);
            cursorY = thingDefRect.yMax + 8f;

            // Edit button
            var editConfigRect = new Rect(rect.x, cursorY, 200f, BottomButtonsHeight);
            DrawActionButton(editConfigRect, "Edit Collection Config", () =>
            {
                Find.WindowStack.Add(new CollectionDefConfigEditorWindow(typedSettings.Config, config =>
                {
                    typedSettings.Config = config;
                }));
            });
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
        public string GetShortDescription() => "Filter for matching thing(defs) based on specified conditions on their properties and/or other collections";
        /// <inheritdoc/>
        public string GetLongDescription(IExposable settings)
        {
            if (settings is null) return GetShortDescription();
            if (settings is not ComplexFilterPolicySettings typedSettings) return GetShortDescription();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Match all").Append(typedSettings.ThingDef ? " ThingDefs " : " Things ")
                .Append("that satisfy the following conditions:").AppendLine();
            var errors = ValidateSettings(settings).ToArray();
            if (errors.Length > 0)
            {
                stringBuilder.AppendLine("Errors in settings. Can't display");
            }
            else
            {
                _ = typedSettings.Collection.ToString(stringBuilder);

            }
            return stringBuilder.ToString();
        }
        /// <inheritdoc/>
        public string GetTitle() => "Complex Filter Policy";

        private class Provider : IDynamicPolicyProvider
        {
            // Fields
            private readonly ComplexFilterPolicy _parent;
            private readonly ComplexFilterPolicySettings _settings;

            public Provider(ComplexFilterPolicy parent, ComplexFilterPolicySettings settings)
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
                    x.FromDef(_settings.Collection);
                    return _settings.ThingDef ? 
                        x.CollectFromSnapshot(d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName), d => d.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName).Enumerate<IIndexed<ThingDef>>(), false) : 
                        x.CollectFromSnapshot(d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName), d => d.GetTable<Thing>(Toolkit.Indexing.Thing.TableName).Enumerate<IIndexed<Thing>>());
                });

                context.WithLabel("Complex Filter")
                       .WithDescription(_parent.GetLongDescription(_settings));

                if (_settings.ThingDef)
                {
                    context.AvailableFor<Map, ThingDef>(new CollectionPolicy(name));
                }
                else
                {
                    context.AvailableFor<Map, Thing>(new CollectionPolicy(name));
                }
            }
            /// <inheritdoc/>
            public void Deactivate(Action disposePolicies)
            {
            }
        }
    }
    /// <summary>
    /// Contains the settings for the <see cref="ComplexFilterPolicy"/>. This includes the conditions, inclusions, and exclusions that define the filter behavior.
    /// </summary>
    public class ComplexFilterPolicySettings : IExposable
    {
        // Fields
        private CollectionDef _staticDef;
        /// <summary>
        /// Filter applies to <see cref="Verse.ThingDef"/>s. Default is true.
        /// When false it applies to <see cref="Verse.Thing"/>.
        /// </summary>
        public bool ThingDef = true;
        /// <summary>
        /// The config backing the complex filter policy.
        /// </summary>
        public CollectionDefConfig Config = new CollectionDefConfig();

        // Properties
        /// <summary>
        /// The collection definition for the complex filter policy.
        /// </summary>
        public CollectionDef Collection => _staticDef ?? Config.ToDef();

        /// <summary>
        /// Creates a new instance of ComplexFilterPolicySettings from a static CollectionDef.
        /// </summary>
        /// <param name="def">The static CollectionDef to use.</param>
        /// <returns>A new instance of ComplexFilterPolicySettings.</returns>
        public static ComplexFilterPolicySettings FromStatic(CollectionDef def)
        {
            return new ComplexFilterPolicySettings
            {
                _staticDef = def
            };
        }

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref ThingDef, "ThingDef", true);
            Scribe_Deep.Look(ref Config, "Config");
            Config ??= new CollectionDefConfig();

        }
    }
}
