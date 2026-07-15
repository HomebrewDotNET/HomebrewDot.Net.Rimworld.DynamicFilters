using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    /// <summary>
    /// Shared fixture that sets up the indexing environment once for all DynamicFilterPresets tests.
    /// Mirrors game semantics: StartIndexing is called once per save load, and all presets share the same state.
    /// </summary>
    public class DynamicFilterPresetsFixture : IDisposable
    {
        public const string MeatDefName = "Test_Meat";
        public const string MetalDefName = "Test_Metal";
        public const string IngestibleDefName = "Test_Ingestible";
        public const string FoodDefName = "Test_Food";
        public const string MealSimpleDefName = "Test_MealSimple";
        public const string MealLavishDefName = "Test_MealLavish";
        public const string MealAwfulDefName = "Test_MealAwful";
        public const string SnackJoyDefName = "Test_SnackJoy";
        public const string SnackRawTastyDefName = "Test_SnackRawTasty";
        public const string MedicineDefName = "Test_Medicine";
        public const string ApparelDefName = "Test_Apparel";
        public const string MeleeWeaponDefName = "Test_MeleeWeapon";
        public const string RangedWeaponDefName = "Test_RangedWeapon";
        public const string FlammableDefName = "Test_Flammable";
        public const string NonFlammableDefName = "Test_NonFlammable";
        public const string ConstructionDefName = "Test_Construction";
        public const string ExplosiveDefName = "Test_Explosive";
        public const string GenericItemDefName = "Test_GenericItem";

        public const string TentityMatchingText = "matching";
        public const string TentityNonMatchingText = "non-matching";

        // Self-owned defs created via GetUninitializedObject (avoids Unity-dependent ctors)
        public static readonly ThingCategoryDef MeatRawDef = MakeDef<ThingCategoryDef>("Test_MeatRaw");
        public static readonly StuffCategoryDef MetallicDef = MakeDef<StuffCategoryDef>("Test_Metallic");
        public static readonly StatDef MedicalPotencyDef = MakeDef<StatDef>("Test_MedicalPotency");
        public static readonly StatDef FlammabilityDef = MakeDef<StatDef>("Test_Flammability");

        // Reflection for read-only backing fields
        private static readonly FieldInfo CachedNutritionField = typeof(IngestibleProperties)
            .GetField("<CachedNutrition>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(IngestibleProperties).GetField("cachedNutrition", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo VerbsField = typeof(ThingDef)
            .GetField("verbs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo IsMeleeAttackField = typeof(VerbProperties)
            .GetField("<IsMeleeAttack>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(VerbProperties).GetField("isMeleeAttack", BindingFlags.Instance | BindingFlags.NonPublic);

        public DynamicFilterPresetsFixture()
        {
            ConfigureServices();
            Indexing.ConfigureSchema += ConfigureTentitySchema;
            SetupIndexing();
            PushTentityData();
            Indexing.Orchestrator.ForceSnapshot();
        }

        public void Dispose()
        {
            Indexing.ConfigureSchema -= ConfigureTentitySchema;
            InvokeSafe(() => Indexing.Orchestrator?.ForceSnapshot());
            InvokeSafe(() => Collecting.ReloadDefaultComparator());
        }

        private static void ConfigureTentitySchema(IDatabaseSchemaBuilder schema)
        {
            schema.WithTable<Tentity<bool>>("TentityBool")
                  .WithTable<Tentity<float>>("TentityFloat");
        }

        // ── Helpers ──

        public static void InvokeSafe(Action action)
        {
            try { action(); } catch { /* cleanup best-effort */ }
        }

        public static T MakeDef<T>(string defName) where T : Def
        {
            var def = (T)FormatterServices.GetUninitializedObject(typeof(T));
            def.defName = defName;
            return def;
        }

        private static void SetCachedNutrition(IngestibleProperties props, float value)
        {
            if (CachedNutritionField != null)
                CachedNutritionField.SetValue(props, value);
        }

        private static void SetupIndexing()
        {
            Indexing.Def.Thing.EnsureTable();
            // Use Include indexer so test metadata (pushed via PushDef) flows through to the indexed item.
            // TrackIsConstructionMaterial() is not used because it depends on Current.Game and real DefDatabase.
            Toolkit.Indexing.Indexers.BuildIndexer<ThingDef>(ToolkitConstants.Def.Thing.IsConstructionMaterial.Name, x => x.Include<bool>(ToolkitConstants.Def.Thing.IsConstructionMaterial, true));
            Indexing.StartIndexing(null, true);

            ushort counter = 0;
            var metadata = new IndexMetadata();

            PushDef(MeatDefName, "meat", counter++, def =>
            {
                def.category = ThingCategory.Item;
                def.thingCategories = new List<ThingCategoryDef> { MeatRawDef };
            }, ref metadata);

            PushDef(MetalDefName, "metal", counter++, def =>
            {
                def.stuffProps = new StuffProperties
                {
                    categories = new List<StuffCategoryDef> { MetallicDef }
                };
            }, ref metadata);

            PushDef(IngestibleDefName, "ingestible", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
            }, ref metadata);

            PushDef(FoodDefName, "food", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 0.5f);
            }, ref metadata);

            PushDef(MealSimpleDefName, "meal simple", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 0.9f);
                def.ingestible.preferability = FoodPreferability.MealSimple;
            }, ref metadata);

            PushDef(MealLavishDefName, "meal lavish", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 1.0f);
                def.ingestible.preferability = FoodPreferability.MealLavish;
            }, ref metadata);

            PushDef(MealAwfulDefName, "meal awful", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 0.5f);
                def.ingestible.preferability = FoodPreferability.MealAwful;
            }, ref metadata);

            PushDef(SnackJoyDefName, "snack joy", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 0.1f);
                def.ingestible.joy = 0.2f;
            }, ref metadata);

            PushDef(SnackRawTastyDefName, "snack raw tasty", counter++, def =>
            {
                def.ingestible = new IngestibleProperties();
                SetCachedNutrition(def.ingestible, 0.1f);
                def.ingestible.preferability = FoodPreferability.RawTasty;
            }, ref metadata);

            PushDef(MedicineDefName, "medicine", counter++, def =>
            {
                def.statBases = new List<StatModifier>
                {
                    new StatModifier { stat = MedicalPotencyDef, value = 1.0f }
                };
            }, ref metadata);

            PushDef(ApparelDefName, "apparel", counter++, def =>
            {
                def.apparel = new ApparelProperties();
            }, ref metadata);

            PushDef(MeleeWeaponDefName, "melee weapon", counter++, def =>
            {
                def.category = ThingCategory.Item;
                def.tools = new List<Tool> { new Tool() };
            }, ref metadata);

            PushDef(RangedWeaponDefName, "ranged weapon", counter++, def =>
            {
                def.category = ThingCategory.Item;
                var verbProps = new VerbProperties();
                IsMeleeAttackField?.SetValue(verbProps, false);
                VerbsField?.SetValue(def, new List<VerbProperties> { verbProps });
            }, ref metadata);

            PushDef(FlammableDefName, "flammable", counter++, def =>
            {
                def.statBases = new List<StatModifier>
                {
                    new StatModifier { stat = FlammabilityDef, value = 1.0f }
                };
            }, ref metadata);

            PushDef(NonFlammableDefName, "non-flammable", counter++, def =>
            {
                def.statBases = new List<StatModifier>
                {
                    new StatModifier { stat = FlammabilityDef, value = 0f }
                };
            }, ref metadata);

            metadata.Set(ToolkitConstants.Def.Thing.IsConstructionMaterial, true, true);
            PushDef(ConstructionDefName, "construction", counter++, _ => { }
            , ref metadata);
            metadata = new IndexMetadata();
            
            PushDef(ExplosiveDefName, "explosive", counter++, def =>
            {
                def.comps = new List<CompProperties>
                {
                    new CompProperties_Explosive { explodeOnDestroyed = true }
                };
            }, ref metadata);

            PushDef(GenericItemDefName, "generic item", counter++, def =>
            {
                def.category = ThingCategory.Item;
            }, ref metadata);

            Indexing.Orchestrator.ForceSnapshot();
        }

        private static void PushDef(string defName, string label, ushort index, Action<ThingDef> configure,
            ref IndexMetadata metadata)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            def.label = label;
            def.index = index;
            def.defNameHash = (ushort)(index + 1);
            configure(def);
            Indexing.Manager.Push(def, ref metadata);
        }

        /// <summary>Pushes Tentity instances so collector-based tests can find them.</summary>
        private static void PushTentityData()
        {
            var metadata = new IndexMetadata();
            Indexing.Manager.Push(new Tentity<bool> { number = 1, boolean = true, text = TentityMatchingText }, ref metadata);
            Indexing.Manager.Push(new Tentity<bool> { number = 2, boolean = false, text = TentityNonMatchingText }, ref metadata);
            Indexing.Manager.Push(new Tentity<float> { number = 3, floatingNumber = 1.5f, text = TentityMatchingText }, ref metadata);
            Indexing.Manager.Push(new Tentity<float> { number = 4, floatingNumber = 0f, text = TentityNonMatchingText }, ref metadata);
        }
    }
}
