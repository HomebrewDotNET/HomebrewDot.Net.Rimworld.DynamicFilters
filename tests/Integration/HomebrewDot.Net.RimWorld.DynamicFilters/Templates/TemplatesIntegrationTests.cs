using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Configuration;
using Moq;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Templates
{
    [Trait("Category", "Integration")]
    public class TemplatesIntegrationTests : IDisposable
    {
        private static readonly FieldInfo TemplatesField = ResolveTemplatesField();

        public TemplatesIntegrationTests()
        {
            ResetTemplates();
        }

        public void Dispose()
        {
            ResetTemplates();
        }

        private static void ResetTemplates()
        {
            try
            {
                var field = TemplatesField;
                field?.SetValue(null, new HashSet<IDynamicPolicyTemplate>());
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

        [Fact]
        public void Templates_AddTemplate_WhenNew_AppearsInAll()
        {
            // Arrange
            var template = CreateTemplate("key1", "Title1");

            // Act
            DynamicFiltersToolkit.Templates.AddTemplate(template);

            // Assert
            Assert.Contains(template, DynamicFiltersToolkit.Templates.All);
        }

        [Fact]
        public void Templates_AddTemplate_WhenDuplicate_DoesNotAddAgain()
        {
            // Arrange
            var template = CreateTemplate("dup_key", "Dup");

            // Act
            DynamicFiltersToolkit.Templates.AddTemplate(template);
            var countAfterFirst = DynamicFiltersToolkit.Templates.All.Count;
            DynamicFiltersToolkit.Templates.AddTemplate(template);
            var countAfterSecond = DynamicFiltersToolkit.Templates.All.Count;

            // Assert
            Assert.Equal(countAfterFirst, countAfterSecond);
        }

        [Fact]
        public void Templates_All_WhenEmpty_ReturnsEmptyCollection()
        {
            // Arrange - already reset in constructor

            // Act
            var all = DynamicFiltersToolkit.Templates.All;

            // Assert
            Assert.Empty(all);
        }

        [Fact]
        public void Templates_All_ReturnsOrderedByStorageKey()
        {
            // Arrange
            var t1 = CreateTemplate("zebra_key", "Zebra");
            var t2 = CreateTemplate("alpha_key", "Alpha");
            var t3 = CreateTemplate("middle_key", "Middle");
            DynamicFiltersToolkit.Templates.AddTemplate(t1);
            DynamicFiltersToolkit.Templates.AddTemplate(t2);
            DynamicFiltersToolkit.Templates.AddTemplate(t3);

            // Act
            var all = DynamicFiltersToolkit.Templates.All.ToList();

            // Assert
            Assert.Equal("alpha_key", all[0].StorageKey);
            Assert.Equal("middle_key", all[1].StorageKey);
            Assert.Equal("zebra_key", all[2].StorageKey);
        }

        private static IDynamicPolicyTemplate CreateTemplate(string storageKey, string title)
        {
            var mock = new Mock<IDynamicPolicyTemplate>();
            mock.SetupGet(t => t.StorageKey).Returns(storageKey);
            mock.Setup(t => t.GetTitle()).Returns(title);
            mock.Setup(t => t.GetShortDescription()).Returns("");
            mock.Setup(t => t.GetLongDescription(It.IsAny<IExposable>())).Returns("");
            return mock.Object;
        }
    }
}
