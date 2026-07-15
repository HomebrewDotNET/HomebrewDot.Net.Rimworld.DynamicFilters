using System;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Filtering;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Policies
{
    [Trait("Category", "Integration")]
    public class PoliciesIntegrationTests : IDisposable
    {
        public PoliciesIntegrationTests()
        {
            // Ensure a clean start
            CleanupAllPolicies();
        }

        public void Dispose()
        {
            CleanupAllPolicies();
        }

        private static void CleanupAllPolicies()
        {
            try
            {
                var active = DynamicFiltersToolkit.Policies.ActivePolicies.ToList();
                foreach (var name in active)
                {
                    InvokeSafe(() => DynamicFiltersToolkit.Policies.DeactivateProvider(name));
                }
            }
            catch { }
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        private static IDynamicPolicyProvider CreateProvider(Action<string, IDynamicPolicyProviderActivationContext> activateCallback = null, Action<Action> deactivateCallback = null)
        {
            var mock = new Mock<IDynamicPolicyProvider>();
            mock.Setup(p => p.Activate(It.IsAny<string>(), It.IsAny<IDynamicPolicyProviderActivationContext>()))
                .Callback((string name, IDynamicPolicyProviderActivationContext ctx) =>
                {
                    activateCallback?.Invoke(name, ctx);
                });
            mock.Setup(p => p.Deactivate(It.IsAny<Action>()))
                .Callback((Action dispose) =>
                {
                    deactivateCallback?.Invoke(dispose);
                });
            return mock.Object;
        }

        [Fact]
        public void Policies_TryActivateProvider_NewName_ReturnsTrue()
        {
            // Arrange
            IDynamicPolicyProviderActivationContext capturedCtx = null;
            var provider = CreateProvider(activateCallback: (n, ctx) => { capturedCtx = ctx; });

            // Act
            var result = DynamicFiltersToolkit.Policies.TryActivateProvider("policies_test_new", provider);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedCtx);
        }

        [Fact]
        public void Policies_TryActivateProvider_Duplicate_WithoutDeactivate_ReturnsFalse()
        {
            // Arrange
            var providerA = CreateProvider();
            var providerB = CreateProvider();
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_dup_test", providerA);

            // Act
            var result = DynamicFiltersToolkit.Policies.TryActivateProvider("policies_dup_test", providerB);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Policies_TryActivateProvider_Duplicate_WithDeactivate_Replaces()
        {
            // Arrange
            var providerA = CreateProvider();
            var providerB = CreateProvider();
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_replace_test", providerA);

            // Act
            var result = DynamicFiltersToolkit.Policies.TryActivateProvider("policies_replace_test", providerB, deactivateExisting: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Policies_DeactivateProvider_ActiveProvider_RemovesFromActive()
        {
            // Arrange
            var provider = CreateProvider();
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_deact_active", provider);

            // Act
            DynamicFiltersToolkit.Policies.DeactivateProvider("policies_deact_active");

            // Assert
            Assert.DoesNotContain("policies_deact_active", DynamicFiltersToolkit.Policies.ActivePolicies);
        }

        [Fact]
        public void Policies_DeactivateProvider_NotActive_DoesNotThrow()
        {
            // Act & Assert
            var ex = Record.Exception(() => DynamicFiltersToolkit.Policies.DeactivateProvider("policies_deact_notactive_xyz"));
            Assert.Null(ex);
        }

        [Fact]
        public void Policies_ActivePolicies_AfterActivation_ContainsName()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_contains_name", provider);

            // Assert
            Assert.Contains("policies_contains_name", DynamicFiltersToolkit.Policies.ActivePolicies);
        }

        [Fact]
        public void Policies_ActivePoliciesInfo_ContainsLabelAndDescription()
        {
            // Arrange
            var provider = CreateProvider(activateCallback: (n, ctx) =>
            {
                ctx.WithLabel("My Label");
                ctx.WithDescription("My Description");
            });
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_info_test", provider);

            // Act
            var info = DynamicFiltersToolkit.Policies.ActivePoliciesInfo.FirstOrDefault(p => p.Name == "policies_info_test");

            // Assert
            Assert.NotNull(info);
            Assert.Equal("My Label", info.Label);
            Assert.Equal("My Description", info.Description);
        }

        [Fact]
        public void Policies_TryActivateProvider_IsReadOnly_PropagatesToInfo()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            DynamicFiltersToolkit.Policies.TryActivateProvider("policies_readonly_test", provider, deactivateExisting: false, isReadOnly: true);

            // Assert
            var info = DynamicFiltersToolkit.Policies.ActivePoliciesInfo.FirstOrDefault(p => p.Name == "policies_readonly_test");
            Assert.NotNull(info);
            Assert.True(info.IsReadOnly);
        }
    }
}
