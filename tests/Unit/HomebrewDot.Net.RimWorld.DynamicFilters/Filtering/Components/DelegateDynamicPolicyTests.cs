using System;
using HomebrewDot.Net.Rimworld.Filtering;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Components
{
    public class DelegateDynamicPolicyTests
    {
        [Fact]
        public void Constructor_WithValidArguments_SetsName()
        {
            // Arrange
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                var mockFilter = new Mock<IDynamicFilter<string, int>>();
                return mockFilter.Object;
            };

            // Act
            var policy = new DelegateDynamicPolicy<string, int>("TestPolicy", factory);

            // Assert
            Assert.Equal("TestPolicy", policy.Name);
        }

        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                var mockFilter = new Mock<IDynamicFilter<string, int>>();
                return mockFilter.Object;
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DelegateDynamicPolicy<string, int>(null, factory));
        }

        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                var mockFilter = new Mock<IDynamicFilter<string, int>>();
                return mockFilter.Object;
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new DelegateDynamicPolicy<string, int>("", factory));
        }

        [Fact]
        public void Constructor_WithWhitespaceName_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                var mockFilter = new Mock<IDynamicFilter<string, int>>();
                return mockFilter.Object;
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new DelegateDynamicPolicy<string, int>("   ", factory));
        }

        [Fact]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DelegateDynamicPolicy<string, int>("TestPolicy", null));
        }

        [Fact]
        public void GetFilter_ReturnsFilterFromFactory()
        {
            // Arrange
            var expectedFilter = new Mock<IDynamicFilter<string, int>>().Object;
            Func<string, IDynamicFilter<string, int>> factory = scope => expectedFilter;
            var policy = new DelegateDynamicPolicy<string, int>("TestPolicy", factory);

            // Act
            var result = policy.GetFilter("my-scope");

            // Assert
            Assert.Same(expectedFilter, result);
        }

        [Fact]
        public void GetFilter_PassesScopeToFactory()
        {
            // Arrange
            var capturedScope = default(string);
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                capturedScope = scope;
                return new Mock<IDynamicFilter<string, int>>().Object;
            };
            var policy = new DelegateDynamicPolicy<string, int>("TestPolicy", factory);

            // Act
            policy.GetFilter("scope-value");

            // Assert
            Assert.Equal("scope-value", capturedScope);
        }

        [Fact]
        public void GetFilter_CalledMultipleTimes_InvokesFactoryEachTime()
        {
            // Arrange
            var callCount = 0;
            Func<string, IDynamicFilter<string, int>> factory = scope =>
            {
                callCount++;
                return new Mock<IDynamicFilter<string, int>>().Object;
            };
            var policy = new DelegateDynamicPolicy<string, int>("TestPolicy", factory);

            // Act
            policy.GetFilter("a");
            policy.GetFilter("b");
            policy.GetFilter("c");

            // Assert
            Assert.Equal(3, callCount);
        }
    }
}
