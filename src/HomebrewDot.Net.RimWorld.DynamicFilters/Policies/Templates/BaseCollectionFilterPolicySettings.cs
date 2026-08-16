using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.Rimworld.Policies.Templates
{
    /// <summary>
    /// Base class for settings used by collection filter policies containing common properties and methods for serialization.
    /// </summary>
    public abstract class BaseCollectionFilterPolicySettings : IExposable
    {
        /// <summary>
        /// Filter applies to <see cref="Verse.ThingDef"/>s. Default is true.
        /// When false it applies to <see cref="Verse.Thing"/>.
        /// </summary>
        public bool ThingDef = true;
        /// <summary>
        /// If filters on <see cref="Thing"/> should be evaluated lazily. Default is true.
        /// This checks the conditions inside the filter instead of precomputing the results and storing them in a collection.
        /// </summary>
        public bool LazyEvaluation = true;
        /// <summary>
        /// If a collection scoped to the map is required. When set to false the global collection will be used instead.
        /// Only applies to <see cref="Thing"/> filters. Default is false.
        /// </summary>
        public bool RequireMapContext = false;

        /// <inheritdoc/>
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref ThingDef, "ThingDef");
            Scribe_Values.Look(ref LazyEvaluation, "LazyEvaluation");
            Scribe_Values.Look(ref RequireMapContext, "RequireMapContext");
        }
    }
}
