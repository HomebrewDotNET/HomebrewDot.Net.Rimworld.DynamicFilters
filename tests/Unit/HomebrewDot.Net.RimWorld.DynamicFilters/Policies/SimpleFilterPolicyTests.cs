using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Policies;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    [Trait("Category", "Unit")]
    public class SimpleFilterPolicyTests
    {
        public SimpleFilterPolicyTests()
        {
            // SimpleFilterPolicy.Instance works without Toolkit.ConfigureServices
        }

        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            // Act
            var a = SimpleFilterPolicy.Instance;
            var b = SimpleFilterPolicy.Instance;

            // Assert
            Assert.Same(a, b);
        }

        [Fact]
        public void StorageKey_ContainsModId()
        {
            // Act
            var key = SimpleFilterPolicy.Instance.StorageKey;

            // Assert
            Assert.Contains(DynamicFiltersToolkit.ModId, key);
        }

        [Fact]
        public void ValidateSettings_WithNull_ReturnsError()
        {
            // Act
            var errors = SimpleFilterPolicy.Instance.ValidateSettings(null).ToList();

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithEmptyConditions_ReturnsError()
        {
            // Arrange
            var settings = new SimpleFilterPolicySettings
            {
                Conditions = new List<SimpleFilterPolicyCondition>()
            };

            // Act
            var errors = SimpleFilterPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithInvalidPath_ReturnsRegexError()
        {
            // Arrange
            var settings = new SimpleFilterPolicySettings
            {
                Conditions = new List<SimpleFilterPolicyCondition>
                {
                    new SimpleFilterPolicyCondition
                    {
                        Config = new ConditionDefConfig
                        {
                            CompareDefault = "invalid path with spaces",
                            CompareType = "Property",
                            Operator = EqualsOperatorType.DefaultTypeName,
                        }
                    }
                }
            };

            // Act
            var errors = SimpleFilterPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithUnknownOperator_ReturnsError()
        {
            // Arrange
            var settings = new SimpleFilterPolicySettings
            {
                Conditions = new List<SimpleFilterPolicyCondition>
                {
                    new SimpleFilterPolicyCondition
                    {
                        Config = new ConditionDefConfig
                        {
                            CompareDefault = "Number",
                            CompareType = "Property",
                            Operator = "DoesNotExist_Operator",
                        }
                    }
                }
            };

            // Act
            var errors = SimpleFilterPolicy.Instance.ValidateSettings(settings).ToList();

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Create_WithWrongType_Throws()
        {
            // Arrange
            var wrongSettings = new WrongSettings();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => SimpleFilterPolicy.Instance.Create(wrongSettings));
        }

        [Fact]
        public void GetTitle_ReturnsNonEmpty()
        {
            // Act
            var title = SimpleFilterPolicy.Instance.GetTitle();

            // Assert
            Assert.False(string.IsNullOrEmpty(title));
        }

        private class WrongSettings : IExposable
        {
            public void ExposeData() { }
        }
    }
}
