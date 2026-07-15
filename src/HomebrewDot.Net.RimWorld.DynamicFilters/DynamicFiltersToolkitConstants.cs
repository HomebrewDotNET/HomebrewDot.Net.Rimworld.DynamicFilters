using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Read only constants related to the dynamic filters toolkit.
    /// </summary>
    public static class DynamicFiltersToolkitConstants
    {
        /// <summary>
        /// Constants relatehe built in filter policies.
        /// </summary>
        public static class Policy
        {
            /// <summary>
            /// Regex pattern for validating property paths that point to nested properties.
            /// Can only be a sequence of text separated by dots. For example: "def.Label".
            /// </summary>
            public const string PropertyPathRegex = @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$";
        }

        /// <summary>
        /// Constants related to <see cref="Verse.ThingFilter"/>s.
        /// </summary>
        public static class ThingFilter
        {
            /// <summary>
            /// Metadata key that stores the storage id of the holder of a <see cref="Verse.ThingFilter"/>.
            /// </summary>
            public static readonly IndexMetadataKey<string> StorageIdKey = IndexMetadataKey<string>.Get("DynamicFilters.StorageId");
            /// <summary>
            /// Metadata key that stores the storage holder of a <see cref="Verse.ThingFilter"/>.
            /// </summary>
            public static readonly IndexMetadataKey<string> StorageKey = IndexMetadataKey<string>.Get("DynamicFilters.Storage");
        }
    }
}
