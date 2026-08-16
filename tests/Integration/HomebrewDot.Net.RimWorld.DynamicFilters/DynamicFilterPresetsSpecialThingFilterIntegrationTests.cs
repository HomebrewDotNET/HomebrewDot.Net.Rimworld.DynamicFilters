using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Configuration;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using RimWorld;
using Verse;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Integration tests for the special thing filter presets: registering a single
    /// <see cref="SpecialThingFilterDef"/> creates a read-only preset template with a
    /// <c>Self MatchesThingFilter [SpecialThingFilterDef]</c> condition, while broken defs are skipped.
    /// </summary>
    [Trait("Category", "Integration")]
    public class DynamicFilterPresetsSpecialThingFilterIntegrationTests : IDisposable
    {
        private static readonly FieldInfo TemplatesField = ResolveTemplatesField();

        public DynamicFilterPresetsSpecialThingFilterIntegrationTests()
        {
            ResetTemplates();
            // Registers all reference and operator types, including the MatchesThingFilter operator and the
            // SpecialThingFilterDef reference.
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            ResetTemplates();
        }

        [Fact]
        public void CreateSpecialThingFilterPreset_RegistersTemplateWithTitle()
        {
            var def = MakeDef("AllowFresh", "allow fresh", typeof(AlwaysTrueWorker));

            DynamicFilterPresets.CreateSpecialThingFilterPreset(def);

            var template = DynamicFiltersToolkit.Templates.All.SingleOrDefault();
            Assert.NotNull(template);
            Assert.Equal("[ThingFilter] Allow Fresh", template.GetTitle());
        }

        [Fact]
        public void CreateSpecialThingFilterPreset_DuplicateDef_StillRegisters()
        {
            // Duplicate defs are registered too: the special thing filter preset wins over the built-in preset
            // (the built-in preset yields to it instead).
            var def = MakeDef("AllowRotten", "allow rotten", typeof(AlwaysTrueWorker));

            DynamicFilterPresets.CreateSpecialThingFilterPreset(def);

            var template = DynamicFiltersToolkit.Templates.All.SingleOrDefault();
            Assert.NotNull(template);
        }

        [Fact]
        public void CreateSpecialThingFilterPreset_NoWorkerClass_RegistersNothing()
        {
            var def = MakeDef("BrokenFilter", "broken filter", null);

            DynamicFilterPresets.CreateSpecialThingFilterPreset(def);

            Assert.Empty(DynamicFiltersToolkit.Templates.All);
        }

        [Fact]
        public void CreateSpecialThingFilterPreset_NullDef_RegistersNothing()
        {
            DynamicFilterPresets.CreateSpecialThingFilterPreset(null);

            Assert.Empty(DynamicFiltersToolkit.Templates.All);
        }

        [Fact]
        public void CreateSpecialThingFilterPreset_SameLabelTwice_DisambiguatesTitles()
        {
            var apparel = MakeDef("TestAllowSmeltableApparel", "allow smeltable", typeof(AlwaysTrueWorker));
            var weapons = MakeDef("TestAllowSmeltableWeapons", "allow smeltable", typeof(AlwaysTrueWorker));
            var apparelCategory = MakeCategory("Apparel");
            var weaponsCategory = MakeCategory("Weapons");
            apparel.parentCategory = apparelCategory;
            weapons.parentCategory = weaponsCategory;
            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DynamicFilterPresets.CreateSpecialThingFilterPreset(apparel, usedTitles);
            DynamicFilterPresets.CreateSpecialThingFilterPreset(weapons, usedTitles);

            var titles = DynamicFiltersToolkit.Templates.All.Select(x => x.GetTitle()).ToList();
            Assert.Equal(2, titles.Count);
            Assert.Contains("[ThingFilter] Allow Smeltable", titles);
            Assert.Contains("[ThingFilter] Allow Smeltable (Weapons)", titles);
        }

        // ── Helpers ──

        private static void ResetTemplates()
        {
            try
            {
                TemplatesField?.SetValue(null, new HashSet<IDynamicPolicyTemplate>());
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private static FieldInfo ResolveTemplatesField()
        {
            var asm = typeof(DynamicFiltersToolkit).Assembly;
            var toolkitType = asm.GetType("HomebrewDot.Net.Rimworld.DynamicFiltersToolkit");
            if (toolkitType == null) return null;
            var templatesType = toolkitType.GetNestedType("Templates", BindingFlags.NonPublic | BindingFlags.Public);
            if (templatesType == null) return null;
            return templatesType.GetField("_templates", BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static SpecialThingFilterDef MakeDef(string defName, string label, Type workerClass)
        {
            var def = (SpecialThingFilterDef)FormatterServices.GetUninitializedObject(typeof(SpecialThingFilterDef));
            def.defName = defName;
            def.label = label;
            def.workerClass = workerClass;
            return def;
        }

        private static ThingCategoryDef MakeCategory(string defName)
        {
            var category = (ThingCategoryDef)FormatterServices.GetUninitializedObject(typeof(ThingCategoryDef));
            category.defName = defName;
            category.label = defName;
            return category;
        }

        public sealed class AlwaysTrueWorker : SpecialThingFilterWorker
        {
            public override bool Matches(Thing t)
            {
                return true;
            }
        }
    }
}
