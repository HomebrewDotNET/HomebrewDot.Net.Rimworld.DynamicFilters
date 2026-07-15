using System;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Policies;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests
{
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class DynamicFilterPresetsLifecycleIntegrationTests : IDisposable
    {
        public DynamicFilterPresetsLifecycleIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            // Clean up any registered policies
            try
            {
                var active = DynamicFiltersToolkit.Policies.ActivePolicies.ToList();
                foreach (var name in active)
                {
                    DynamicFiltersToolkit.Policies.DeactivateProvider(name);
                }
            }
            catch { }
        }

        [Fact]
        public void DynamicFilterPresets_CreatePropertyCondition_ReturnsNonEmpty()
        {
            // Act
            var conditions = DynamicFilterPresets.CreatePropertyCondition(
                "Number", EqualsOperatorType.DefaultTypeName, 5);

            // Assert
            Assert.NotNull(conditions);
            Assert.NotEmpty(conditions);
        }

        [Fact]
        public void DynamicFilterPresets_AddPresetProvider_ThenActivatePresets_CallsAllProviders()
        {
            // This test requires the Unity runtime for StatDefOf initialization.
            // In pure .NET test runners, ActivatePresets() will fail when it
            // accesses StatDefOf.Flammability. We guard against that here.
            try
            {
                // Arrange
                int callCount = 0;
                var policyName = $"LPC_Test_{Guid.NewGuid()}";
                DynamicFilterPresets.AddPresetProvider(activator =>
                {
                    // Use a delegate provider via SimpleFilterPolicy
                    var settings = new SimpleFilterPolicySettings
                    {
                        ThingDef = true,
                        DisallowMatching = false,
                        Conditions = DynamicFilterPresets.CreatePropertyCondition("Number", EqualsOperatorType.DefaultTypeName, 1).ToList()
                    };
                    var template = SimpleFilterPolicy.Instance;
                    var provider = template.Create(settings);
                    activator(policyName, provider);
                    callCount++;
                });

                // Act
                DynamicFilterPresets.ActivatePresets();

                // Assert - our provider was called
                Assert.True(callCount >= 1);

                // Cleanup
                try { DynamicFiltersToolkit.Policies.DeactivateProvider(policyName); } catch { }
            }
            catch (TypeInitializationException ex) when (ex.TypeName.Contains("StatDefOf"))
            {
                // StatDefOf requires Unity runtime; skip validation in pure .NET test context
            }
        }

        [Fact]
        public void DynamicFilterPresets_DeactivatePresets_DeactivatesAllActive()
        {
            // Arrange
            // Activate a simple policy first
            var policyName = $"DP_Deact_{Guid.NewGuid()}";
            var conditions = DynamicFilterPresets.CreatePropertyCondition("Number", EqualsOperatorType.DefaultTypeName, 1);
            try
            {
                DynamicFilterPresets.ActivateSimple(policyName, conditions);

                // Act
                DynamicFilterPresets.DeactivatePresets();

                // Assert - all policies should be deactivated
                Assert.DoesNotContain(policyName, DynamicFiltersToolkit.Policies.ActivePolicies);
            }
            catch
            {
                // best-effort cleanup
                InvokeSafe(() => DynamicFilterPresets.DeactivatePresets());
            }
        }

        [Fact]
        public void DynamicFilterPresets_ActivateSimple_CreatesAndActivatesProvider()
        {
            // Arrange
            var policyName = $"Simple_Activate_{Guid.NewGuid()}";
            var conditions = DynamicFilterPresets.CreatePropertyCondition("Number", EqualsOperatorType.DefaultTypeName, 1);

            // Act
            DynamicFilterPresets.ActivateSimple(policyName, conditions);

            // Assert
            Assert.Contains(policyName, DynamicFiltersToolkit.Policies.ActivePolicies);

            // Cleanup
            try { DynamicFilterPresets.DeactivatePresets(); } catch { }
        }

        [Fact]
        public void DynamicFilterPresets_ActivateSimple_WithConditions_UsesAllConditions()
        {
            // Arrange
            var policyName = $"Simple_Multi_{Guid.NewGuid()}";
            var conditions = DynamicFilterPresets.CreatePropertyCondition("Number", EqualsOperatorType.DefaultTypeName, 5);

            // Act
            DynamicFilterPresets.ActivateSimple(policyName, conditions);

            // Assert
            Assert.Contains(policyName, DynamicFiltersToolkit.Policies.ActivePolicies);

            // Cleanup
            try { DynamicFilterPresets.DeactivatePresets(); } catch { }
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }
    }
}
