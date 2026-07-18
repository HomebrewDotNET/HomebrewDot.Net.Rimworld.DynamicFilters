using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Policies;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    [Trait("Category", "Unit")]
    public class ComplexFilterPolicyTests
    {
        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            var a = ComplexFilterPolicy.Instance;
            var b = ComplexFilterPolicy.Instance;

            Assert.Same(a, b);
        }

        [Fact]
        public void StorageKey_ContainsModId()
        {
            var key = ComplexFilterPolicy.Instance.StorageKey;

            Assert.Contains(DynamicFiltersToolkit.ModId, key);
        }

        [Fact]
        public void Singleton_IsFalse()
        {
            Assert.False(ComplexFilterPolicy.Instance.Singleton);
        }

        [Fact]
        public void ValidateSettings_WithNull_ReturnsError()
        {
            var errors = ComplexFilterPolicy.Instance.ValidateSettings(null).ToList();

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithWrongType_ReturnsError()
        {
            var errors = ComplexFilterPolicy.Instance.ValidateSettings(new SimpleFilterPolicySettings()).ToList();

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithEmptyConfig_ReturnsError()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig()
            };

            var errors = ComplexFilterPolicy.Instance.ValidateSettings(settings).ToList();

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("At least one condition"));
        }

        [Fact]
        public void ValidateSettings_WithConditionHavingEmptyOperator_ReturnsError()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "defName",
                            Operator = string.Empty,
                            ToDefault = "TestValue"
                        }
                    }
                }
            };

            var errors = ComplexFilterPolicy.Instance.ValidateSettings(settings).ToList();

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithInvalidPropertyPath_ReturnsRegexError()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "invalid path with spaces",
                            Operator = "equals",
                            ToDefault = "Value"
                        }
                    }
                }
            };

            var errors = ComplexFilterPolicy.Instance.ValidateSettings(settings).ToList();

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ValidateSettings_WithUnknownOperator_ReturnsError()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "defName",
                            Operator = "nonexistent_operator",
                            ToDefault = "Value"
                        }
                    }
                }
            };

            var errors = ComplexFilterPolicy.Instance.ValidateSettings(settings).ToList();

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Unknown operator"));
        }

        [Fact]
        public void ValidateSettings_WithEmptyCollectionName_ReturnsError()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig
                {
                    Inclusions = new List<CollectionConditionDefConfig>
                    {
                        new CollectionConditionDefConfig { Name = string.Empty }
                    }
                }
            };

            var errors = ComplexFilterPolicy.Instance.ValidateSettings(settings).ToList();

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Collection name cannot be empty"));
        }

        [Fact]
        public void GetShortDescription_ReturnsNonEmptyString()
        {
            var description = ComplexFilterPolicy.Instance.GetShortDescription();

            Assert.False(string.IsNullOrWhiteSpace(description));
        }

        [Fact]
        public void GetLongDescription_WithNullSettings_ReturnsShortDescription()
        {
            var description = ComplexFilterPolicy.Instance.GetLongDescription(null);

            Assert.Equal(ComplexFilterPolicy.Instance.GetShortDescription(), description);
        }

        [Fact]
        public void GetLongDescription_WithInvalidType_ReturnsShortDescription()
        {
            var description = ComplexFilterPolicy.Instance.GetLongDescription(new SimpleFilterPolicySettings());

            Assert.Equal(ComplexFilterPolicy.Instance.GetShortDescription(), description);
        }

        [Fact]
        public void GetLongDescription_WithValidSettings_ReturnsNonEmptyString()
        {
            var settings = new ComplexFilterPolicySettings
            {
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "defName",
                            Operator = "equals",
                            ToDefault = "TestValue"
                        }
                    }
                }
            };

            var description = ComplexFilterPolicy.Instance.GetLongDescription(settings);

            Assert.False(string.IsNullOrWhiteSpace(description));
        }

        [Fact]
        public void GetTitle_ReturnsNonEmptyString()
        {
            var title = ComplexFilterPolicy.Instance.GetTitle();

            Assert.False(string.IsNullOrWhiteSpace(title));
        }
    }
}
