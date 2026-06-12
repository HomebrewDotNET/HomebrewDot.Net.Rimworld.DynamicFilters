using System;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using HomebrewDot.Net.Rimworld.State;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Components
{
    public class DelegateDynamicFilterTests
    {
        [Fact]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            // Arrange
            var scope = "test-scope";
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => i > 0;

            // Act
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter);

            // Assert
            Assert.Equal(scope, dynamicFilter.Scope);
            Assert.Same(policy, dynamicFilter.Policy);
        }

        [Fact]
        public void Constructor_WithNullScope_ThrowsArgumentNullException()
        {
            // Arrange
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => i > 0;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DelegateDynamicFilter<string, int>(null, policy, filter));
        }

        [Fact]
        public void Constructor_WithNullPolicy_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, int, bool> filter = (s, i) => i > 0;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DelegateDynamicFilter<string, int>("scope", null, filter));
        }

        [Fact]
        public void Constructor_WithNullFilter_ThrowsArgumentNullException()
        {
            // Arrange
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DelegateDynamicFilter<string, int>("scope", policy, null));
        }

        [Fact]
        public void Filter_WithMatchingItem_ReturnsTrue()
        {
            // Arrange
            var scope = "active";
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => s == "active" && i > 0;
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter);

            // Act
            var result = dynamicFilter.Filter(42);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Filter_WithNonMatchingItem_ReturnsFalse()
        {
            // Arrange
            var scope = "active";
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => s == "active" && i > 100;
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter);

            // Act
            var result = dynamicFilter.Filter(42);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_WithNullUpdateAction_DoesNotThrow()
        {
            // Arrange
            var scope = "active";
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => true;
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter);
            var stateStore = new Mock<IStateStore<string>>().Object;

            // Act & Assert (no exception)
            dynamicFilter.Update(stateStore);
        }

        [Fact]
        public void Update_WithUpdateAction_InvokesAction()
        {
            // Arrange
            var scope = "active";
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => true;
            var updateInvoked = false;
            Action<string, IStateStore<string>> update = (s, store) => { updateInvoked = true; };
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter, update);
            var stateStore = new Mock<IStateStore<string>>().Object;

            // Act
            dynamicFilter.Update(stateStore);

            // Assert
            Assert.True(updateInvoked);
        }

        [Fact]
        public void Filter_ReceivesScopePassedToConstructor()
        {
            // Arrange
            var scope = "my-scope";
            var capturedScope = default(string);
            var policy = new Mock<IDynamicPolicy<string, int>>().Object;
            Func<string, int, bool> filter = (s, i) => { capturedScope = s; return true; };
            var dynamicFilter = new DelegateDynamicFilter<string, int>(scope, policy, filter);

            // Act
            dynamicFilter.Filter(1);

            // Assert
            Assert.Equal("my-scope", capturedScope);
        }
    }
}
