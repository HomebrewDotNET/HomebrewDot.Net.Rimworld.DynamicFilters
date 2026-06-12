using System;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Models
{
    public class ActivatedPoliciesTests
    {
        [Fact]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;

            // Act
            var policies = new ActivatedPolicies("TestName", provider);

            // Assert
            Assert.Equal("TestName", policies.Name);
            Assert.Same(provider, policies.Provider);
        }

        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ActivatedPolicies(null, provider));
        }

        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ActivatedPolicies("", provider));
        }

        [Fact]
        public void Constructor_WithNullProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ActivatedPolicies("TestName", null));
        }

        [Fact]
        public void Constructor_DefaultLabel_IsProviderTypeName()
        {
            // Arrange - use concrete type for GetType() to work
            var provider = new TestPolicyProvider();

            // Act
            var policies = new ActivatedPolicies("TestName", provider);

            // Assert
            Assert.Equal(nameof(TestPolicyProvider), policies.Label);
            Assert.Equal(nameof(TestPolicyProvider), policies.Title);
        }

        [Fact]
        public void Constructor_DefaultDescription_IsEmpty()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;

            // Act
            var policies = new ActivatedPolicies("TestName", provider);

            // Assert
            Assert.Equal(string.Empty, policies.Description);
        }

        [Fact]
        public void WithLabel_SetsLabel()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;
            var policies = new ActivatedPolicies("TestName", provider);
            var context = (IDynamicPolicyProviderActivationContext)policies;

            // Act
            context.WithLabel("Custom Label");

            // Assert
            Assert.Equal("Custom Label", policies.Label);
        }

        [Fact]
        public void WithLabel_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;
            var policies = new ActivatedPolicies("TestName", provider);
            var context = (IDynamicPolicyProviderActivationContext)policies;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => context.WithLabel(null));
        }

        [Fact]
        public void WithTitle_SetsTitle()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;
            var policies = new ActivatedPolicies("TestName", provider);
            var context = (IDynamicPolicyProviderActivationContext)policies;

            // Act
            context.WithTitle("Custom Title");

            // Assert
            Assert.Equal("Custom Title", policies.Title);
        }

        [Fact]
        public void WithDescription_SetsDescription()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;
            var policies = new ActivatedPolicies("TestName", provider);
            var context = (IDynamicPolicyProviderActivationContext)policies;

            // Act
            context.WithDescription("Custom Description");

            // Assert
            Assert.Equal("Custom Description", policies.Description);
        }

        [Fact]
        public void FluentInterface_Chaining_ReturnsSameContext()
        {
            // Arrange
            var provider = new Mock<IDynamicPolicyProvider>().Object;
            var policies = new ActivatedPolicies("TestName", provider);
            var context = (IDynamicPolicyProviderActivationContext)policies;

            // Act
            var result = context.WithLabel("Lbl").WithTitle("Ttl").WithDescription("Desc");

            // Assert
            Assert.Same(context, result);
            Assert.Equal("Lbl", policies.Label);
            Assert.Equal("Ttl", policies.Title);
            Assert.Equal("Desc", policies.Description);
        }

        // Test provider class for type name verification
        private class TestPolicyProvider : IDynamicPolicyProvider
        {
            public string StorageKey => "test.storage.key";

            public void Activate(string name, IDynamicPolicyProviderActivationContext context)
            {
                throw new NotImplementedException();
            }

            public void Deactivate(Action disposePolicies)
            {
                throw new NotImplementedException();
            }
        }
    }
}
