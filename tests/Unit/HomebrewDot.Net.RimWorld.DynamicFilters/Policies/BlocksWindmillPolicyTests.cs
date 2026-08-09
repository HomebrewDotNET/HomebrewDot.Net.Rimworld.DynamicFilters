using System;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Policies;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    /// <summary>
    /// Tests for the <see cref="BlocksWindmillPolicy"/> preset.
    /// </summary>
    [Trait("Category", "Unit")]
    public class BlocksWindmillPolicyTests
    {
        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            var a = BlocksWindmillPolicy.Instance;
            var b = BlocksWindmillPolicy.Instance;

            Assert.Same(a, b);
        }

        [Fact]
        public void StorageKey_ContainsModId()
        {
            var key = BlocksWindmillPolicy.Instance.StorageKey;

            Assert.Contains(DynamicFiltersToolkit.ModId, key);
        }

        [Fact]
        public void BlocksWind_ThingWithBlockWind_ReturnsTrue()
        {
            var def = MakeDef("Test_BlocksWind_Building");
            def.blockWind = true;

            Assert.True(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_VanillaTree_ReturnsTrue()
        {
            var def = MakePlant("Test_BlocksWind_Tree", plant =>
            {
                plant.harvestTag = "Wood";
            });

            Assert.True(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_TreeWithTreeCategory_ReturnsTrue()
        {
            // Alpha Bees' hive trees set treeCategory Full but use harvestTag "Standard"
            // and no forceIsTree, so PlantProperties.IsTree is false. They must still count.
            var def = MakePlant("Test_BlocksWind_HiveTree", plant =>
            {
                plant.harvestTag = "Standard";
                plant.treeCategory = TreeCategory.Full;
            });

            Assert.True(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_ForceIsTree_ReturnsTrue()
        {
            var def = MakePlant("Test_BlocksWind_ForceTree", plant =>
            {
                plant.forceIsTree = true;
            });

            Assert.True(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_NonTreePlant_ReturnsFalse()
        {
            var def = MakePlant("Test_BlocksWind_Grass", plant =>
            {
                plant.harvestTag = "Standard";
                plant.treeCategory = TreeCategory.None;
            });

            Assert.False(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_PlantWithoutPlantProperties_ReturnsFalse()
        {
            var def = MakeDef("Test_BlocksWind_NoPlant");
            def.category = ThingCategory.Plant;

            Assert.False(BlocksWindmillPolicy.BlocksWind(def));
        }

        [Fact]
        public void BlocksWind_NonPlantThing_ReturnsFalse()
        {
            var def = MakeDef("Test_BlocksWind_Item");
            def.category = ThingCategory.Item;

            Assert.False(BlocksWindmillPolicy.BlocksWind(def));
        }

        private static ThingDef MakeDef(string defName)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            return def;
        }

        private static ThingDef MakePlant(string defName, Action<PlantProperties> configure)
        {
            var def = MakeDef(defName);
            def.category = ThingCategory.Plant;
            def.plant = new PlantProperties();
            configure(def.plant);
            return def;
        }
    }
}
