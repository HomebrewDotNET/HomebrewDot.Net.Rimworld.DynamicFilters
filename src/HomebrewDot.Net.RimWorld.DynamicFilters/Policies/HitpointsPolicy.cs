using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Configuration.Templates;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Policies
{
    /// <summary>
    /// Policy that can filter on min and/or max hitpoints just like the default thing filter panel shows.
    /// </summary>
    public class HitpointsPolicy : PolicyHelper<SimpleFilterPolicy, HitpointsPolicySettings>
    {
        private const int HitpointsRangeControlId = 7931421;

        /// <inheritdoc/>
        public override string StorageKey => $"{DynamicFiltersToolkit.ModId}.{typeof(HitpointsPolicy).Name}";

        /// <summary>
        /// The singleton instance of the <see cref="HitpointsPolicy"/> template.
        /// </summary>
        public static HitpointsPolicy Instance { get; } = new HitpointsPolicy();

        private HitpointsPolicy() : base(SimpleFilterPolicy.Instance)
        {

        }

        /// <inheritdoc/>
        public override IExposable ConvertOptions(HitpointsPolicySettings options)
        {
            options = Guard.NotNull(options, nameof(options));

            var hitpointPercentageMetadataKey = ToolkitConstants.Thing.HitPointPercentage;

            var condition = ConditionBuilder.Build(builder =>
            {
                var hitpointsBuilder = builder.Group(equipment => equipment
                                            .Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.IsApparel)}")
                                            .With.True()
                                            .Or
                                            .Compare.Indexed($"{nameof(Thing.def)}.{nameof(ThingDef.IsWeapon)}")
                                            .With.True());

                if(options.MinimumHitpoints != 0)
                {
                    hitpointsBuilder = hitpointsBuilder.And
                                                   .Compare.Indexed(hitpointPercentageMetadataKey.Name)
                                                   .With.GreaterThanOrEqual()
                                                   .To.Value((float)options.MinimumHitpoints);
                }
                if(options.MaximumHitpoints != 100)
                {
                    hitpointsBuilder = hitpointsBuilder.And
                                                   .Compare.Indexed(hitpointPercentageMetadataKey.Name)
                                                   .With.LessThanOrEqual()
                                                   .To.Value((float)options.MaximumHitpoints);
                }
            });

            var simpleFilterConditions = (condition.Conditions != null && condition.Conditions.Length > 0)
                ? condition.Conditions.Select(x => SimpleFilterPolicyCondition.FromDef(x)).ToList()
                : new List<SimpleFilterPolicyCondition> { SimpleFilterPolicyCondition.FromDef(condition) };

            var settings = new SimpleFilterPolicySettings()
            {
                ThingDef = false,
                RequireMapContext = false,
                LazyEvaluation = options.LazyEvaluation,
                Conditions = simpleFilterConditions
            };
            return settings;
        }
        /// <inheritdoc/>
        public override void DrawSettings(Rect rect, ref IExposable settings)
        {
            if (settings is not HitpointsPolicySettings typedSettings)
            {
                typedSettings = new HitpointsPolicySettings();
                settings = typedSettings;
            }

            var cursorY = rect.y;

            var lazyEvaluationRect = new Rect(rect.x, cursorY, rect.width, 24f);
            Widgets.CheckboxLabeled(lazyEvaluationRect, "Lazy Evaluation", ref typedSettings.LazyEvaluation);
            cursorY = lazyEvaluationRect.yMax + 8f;

            var hitpointsRange = new FloatRange(typedSettings.MinimumHitpoints / 100f, typedSettings.MaximumHitpoints / 100f);
            var hitpointsRect = new Rect(rect.x, cursorY, rect.width, 32f);
            Widgets.FloatRange(hitpointsRect, HitpointsRangeControlId, ref hitpointsRange, 0f, 1f, "HitPoints", ToStringStyle.PercentZero, 0f, GameFont.Small, null, 0.01f);
            typedSettings.MinimumHitpoints = Mathf.RoundToInt(hitpointsRange.min * 100f);
            typedSettings.MaximumHitpoints = Mathf.RoundToInt(hitpointsRange.max * 100f);
        }
        /// <inheritdoc/>
        public override IEnumerable<string> ValidateSettings(HitpointsPolicySettings settings)
        {
            settings = Guard.NotNull(settings, nameof(settings));

            if(settings.MinimumHitpoints > settings.MaximumHitpoints)
            {
                yield return "Minimum hitpoints cannot be higher than the maximum hitpoints";
            }
            if(settings.MinimumHitpoints < 0)
            {
                yield return "Minimum hitpoints cannot be lower than 0";
            }
            if (settings.MaximumHitpoints > 100)
            {
                yield return "Maximum hitpoints cannot be higher than 100";
            }
        }
        /// <inheritdoc/>
        public override string GetTitle() => "HitPoints Filter";
        /// <inheritdoc/>
        public override string GetShortDescription() => "Filters Things based on their current hitpoint percentage (Min and/or Max)";
    }

    /// <summary>
    /// The settings for <see cref="HitpointsPolicy"/>.
    /// </summary>
    public class HitpointsPolicySettings : IExposable
    {
        /// <inheritdoc cref="BaseCollectionFilterPolicySettings.LazyEvaluation"/>
        public bool LazyEvaluation;
        /// <summary>
        /// The minimum quality to match.
        /// </summary>
        public int MinimumHitpoints = 0;
        /// <summary>
        /// The maximum quality to match.
        /// </summary>
        public int MaximumHitpoints = 100;

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref LazyEvaluation, "LazyEvaluation");
            Scribe_Values.Look(ref MinimumHitpoints, "MinimumHitpoints");
            Scribe_Values.Look(ref MaximumHitpoints, "MaximumHitpoints");
        }
    }
}
