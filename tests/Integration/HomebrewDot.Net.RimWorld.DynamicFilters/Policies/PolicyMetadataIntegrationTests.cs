using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Policies;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    /// <summary>
    /// Validates that policies activated from the Simple and Complex filter templates expose their template
    /// label/title/description in <see cref="DynamicFiltersToolkit.Policies.ActivePoliciesInfo"/> for every
    /// evaluation mode, so the Policies tab never falls back to the provider type name ("Provider").
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class PolicyMetadataIntegrationTests : IDisposable
    {
        private readonly List<string> _activatedPolicies = new List<string>();

        public PolicyMetadataIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            foreach (var name in _activatedPolicies)
            {
                InvokeSafe(() => DynamicFiltersToolkit.Policies.DeactivateProvider(name));
                InvokeSafe(() => Toolkit.Collecting.Remove(name));
            }
            _activatedPolicies.Clear();
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private void ActivateAndTrack(string policyName, IDynamicPolicyProvider provider)
        {
            Assert.True(DynamicFiltersToolkit.Policies.TryActivateProvider(policyName, provider));
            _activatedPolicies.Add(policyName);
        }

        private static void AssertMetadata(string policyName, string expectedLabel, string expectedTitle)
        {
            var info = DynamicFiltersToolkit.Policies.ActivePoliciesInfo.FirstOrDefault(x => x.Name == policyName);
            Assert.NotNull(info);
            Assert.Equal(expectedLabel, info.Label);
            Assert.Equal(expectedTitle, info.Title);
            Assert.False(string.IsNullOrWhiteSpace(info.Description));
        }

        [Fact]
        public void ComplexFilterPolicy_Activate_Eager_SetsLabelTitleAndDescription()
        {
            // Arrange
            Indexing.Def.Thing.EnsureTable();
            Indexing.StartIndexing(null, false);
            var policyName = $"Complex_Eager_{Guid.NewGuid()}";
            var settings = new ComplexFilterPolicySettings
            {
                ThingDef = true,
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "defName",
                            Operator = EqualsOperatorType.DefaultTypeName,
                            ToDefault = "TestValue"
                        }
                    }
                }
            };

            // Act
            ActivateAndTrack(policyName, ComplexFilterPolicy.Instance.Create(settings));

            // Assert
            AssertMetadata(policyName, "Complex Filter", "Complex Filter Policy");
        }

        [Fact]
        public void ComplexFilterPolicy_Activate_Lazy_SetsLabelTitleAndDescription()
        {
            // Arrange
            Indexing.Thing.EnsureTable();
            Indexing.StartIndexing(null, false);
            var policyName = $"Complex_Lazy_{Guid.NewGuid()}";
            var settings = new ComplexFilterPolicySettings
            {
                ThingDef = false,
                LazyEvaluation = true,
                Config = new CollectionDefConfig
                {
                    Conditions = new List<ConditionDefConfig>
                    {
                        new ConditionDefConfig
                        {
                            CompareDefault = "defName",
                            Operator = EqualsOperatorType.DefaultTypeName,
                            ToDefault = "TestValue"
                        }
                    }
                }
            };

            // Act
            ActivateAndTrack(policyName, ComplexFilterPolicy.Instance.Create(settings));

            // Assert
            AssertMetadata(policyName, "Complex Filter", "Complex Filter Policy");
        }

        [Fact]
        public void SimpleFilterPolicy_Activate_SetsLabelTitleAndDescription()
        {
            // Arrange
            Indexing.Def.Thing.EnsureTable();
            Indexing.StartIndexing(null, false);
            var policyName = $"Simple_{Guid.NewGuid()}";
            var settings = new SimpleFilterPolicySettings
            {
                ThingDef = true,
                Conditions = DynamicFilterPresets.CreatePropertyCondition("Number", EqualsOperatorType.DefaultTypeName, 1).ToList()
            };

            // Act
            ActivateAndTrack(policyName, SimpleFilterPolicy.Instance.Create(settings));

            // Assert
            AssertMetadata(policyName, "Simple Filter", "Simple Filter Policy");
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }
    }
}
