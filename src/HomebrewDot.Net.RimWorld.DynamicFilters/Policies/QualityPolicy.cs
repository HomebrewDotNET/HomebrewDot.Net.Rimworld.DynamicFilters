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
    /// Policy that can filter on min and/or max quality just like the default thing filter panel shows.
    /// </summary>
    public class QualityPolicy : PolicyHelper<SimpleFilterPolicy, QualityPolicySettings>
    {
        private const int QualityRangeControlId = 7931422;

        /// <inheritdoc/>
        public override string StorageKey => $"{DynamicFiltersToolkit.ModId}.{typeof(QualityPolicy).Name}";

        /// <summary>
        /// The singleton instance of the <see cref="QualityPolicy"/> template.
        /// </summary>
        public static QualityPolicy Instance { get; } = new QualityPolicy();

        private QualityPolicy() : base(SimpleFilterPolicy.Instance)
        {

        }

        /// <inheritdoc/>
        public override IExposable ConvertOptions(QualityPolicySettings options)
        {
            options = Guard.NotNull(options, nameof(options));

            var compReference = $"{typeof(CompQuality).FullName}{CompReferenceType.PathSeparator}{nameof(CompQuality.Quality)}";

            var condition = ConditionBuilder.Build(builder =>
            {
                var qualityBuilder = builder.Compare.Comp(typeof(CompQuality))
                                                    .With.NotNull();

                if(options.MinimumQuality != QualityCategory.Awful)
                {
                    qualityBuilder = qualityBuilder.And
                                                   .Compare.Comp(compReference)
                                                   .With.GreaterThanOrEqual()
                                                   .To.Value(options.MinimumQuality);
                }
                if(options.MaximumQuality != QualityCategory.Legendary)
                {
                    qualityBuilder = qualityBuilder.And
                                                   .Compare.Comp(compReference)
                                                   .With.LessThanOrEqual()
                                                   .To.Value(options.MaximumQuality);
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
            if (settings is not QualityPolicySettings typedSettings)
            {
                typedSettings = new QualityPolicySettings();
                settings = typedSettings;
            }

            var cursorY = rect.y;

            var lazyEvaluationRect = new Rect(rect.x, cursorY, rect.width, 24f);
            Widgets.CheckboxLabeled(lazyEvaluationRect, "Lazy Evaluation", ref typedSettings.LazyEvaluation);
            cursorY = lazyEvaluationRect.yMax + 8f;

            var qualityRange = new QualityRange(typedSettings.MinimumQuality, typedSettings.MaximumQuality);
            var qualityRect = new Rect(rect.x, cursorY, rect.width, 32f);
            Widgets.QualityRange(qualityRect, QualityRangeControlId, ref qualityRange);
            typedSettings.MinimumQuality = qualityRange.min;
            typedSettings.MaximumQuality = qualityRange.max;
        }
        /// <inheritdoc/>
        public override IEnumerable<string> ValidateSettings(QualityPolicySettings settings)
        {
            settings = Guard.NotNull(settings, nameof(settings));

            if (settings.MinimumQuality > settings.MaximumQuality)
            {
                yield return "Minimum quality cannot be higher than the maximum quality";
            }
        }
        /// <inheritdoc/>
        public override string GetTitle() => "Quality Filter";
        /// <inheritdoc/>
        public override string GetShortDescription() => "Filters Things based on their quality (Min and/or Max)";
    }

    /// <summary>
    /// The settings for <see cref="QualityPolicy"/>.
    /// </summary>
    public class QualityPolicySettings : IExposable
    {
        /// <inheritdoc cref="BaseCollectionFilterPolicySettings.LazyEvaluation"/>
        public bool LazyEvaluation;
        /// <summary>
        /// The minimum quality to match.
        /// </summary>
        public QualityCategory MinimumQuality = QualityCategory.Awful;
        /// <summary>
        /// The maximum quality to match.
        /// </summary>
        public QualityCategory MaximumQuality = QualityCategory.Legendary;

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref LazyEvaluation, "LazyEvaluation");
            Scribe_Values.Look(ref MinimumQuality, "MinimumQuality");
            Scribe_Values.Look(ref MaximumQuality, "MaximumQuality");
        }
    }
}
