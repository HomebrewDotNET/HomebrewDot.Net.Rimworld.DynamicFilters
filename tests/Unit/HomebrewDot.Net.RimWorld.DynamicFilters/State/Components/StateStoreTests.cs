using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.State;
using HomebrewDot.Net.Rimworld.State.Components;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.State.Components
{
    public class StateStoreTests
    {
        [Fact]
        public void Constructor_WithInstance_SetsInstance()
        {
            // Arrange
            var instance = "my-instance";

            // Act
            var store = new StateStore<string>(instance);

            // Assert
            Assert.Equal(instance, ((IStateStore<string>)store).Instance);
        }

        [Fact]
        public void Constructor_WithNullInstance_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StateStore<string>(null));
        }

        [Fact]
        public void Root_IsNotNull()
        {
            // Assert
            Assert.NotNull(StateStore<object>.Root);
        }

        [Fact]
        public void Root_Instance_IsNull()
        {
            // Assert
            Assert.Null(((IStateStore<object>)StateStore<object>.Root).Instance);
        }

        [Fact]
        public void AddAndGet_StringKey_StoresAndRetrievesValue()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;

            // Act
            stateStore.Add("key1", "value1");

            // Assert
            Assert.True(stateStore.ContainsKey("key1"));
            Assert.Equal("value1", stateStore["key1"]);
        }

        [Fact]
        public void Indexer_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;

            // Act
            stateStore["key2"] = 42;

            // Assert
            Assert.Equal(42, stateStore["key2"]);
        }

        [Fact]
        public void Remove_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("key", "value");

            // Act
            var removed = stateStore.Remove("key");

            // Assert
            Assert.True(removed);
            Assert.False(stateStore.ContainsKey("key"));
        }

        [Fact]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;

            // Act
            var removed = stateStore.Remove("nonexistent");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("a", 1);
            stateStore.Add("b", 2);

            // Act
            stateStore.Clear();

            // Assert
            Assert.Equal(0, stateStore.Count);
            Assert.False(stateStore.ContainsKey("a"));
            Assert.False(stateStore.ContainsKey("b"));
        }

        [Fact]
        public void Count_ReflectsNumberOfEntries()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;

            // Act
            stateStore.Add("a", 1);
            stateStore.Add("b", 2);

            // Assert
            Assert.Equal(2, stateStore.Count);
        }

        [Fact]
        public void GetChildStore_WithNewInstance_CreatesStore()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;

            // Act
            var childStore = parentStateStore.GetChildStore<int>(42);

            // Assert
            Assert.NotNull(childStore);
            Assert.Equal(42, childStore.Instance);
        }

        [Fact]
        public void GetChildStore_SameInstance_ReturnsSameStore()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;

            // Act
            var child1 = parentStateStore.GetChildStore<int>(42);
            var child2 = parentStateStore.GetChildStore<int>(42);

            // Assert
            Assert.Same(child1, child2);
        }

        [Fact]
        public void GetChildStore_DifferentInstances_ReturnsDifferentStores()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;

            // Act
            var child1 = parentStateStore.GetChildStore<int>(42);
            var child2 = parentStateStore.GetChildStore<int>(99);

            // Assert
            Assert.NotSame(child1, child2);
            Assert.Equal(42, child1.Instance);
            Assert.Equal(99, child2.Instance);
        }

        [Fact]
        public void DestroyChildStore_ExistingInstance_ReturnsTrue()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;
            parentStateStore.GetChildStore<int>(42);

            // Act
            var destroyed = parentStateStore.DestroyChildStore<int>(42);

            // Assert
            Assert.True(destroyed);
        }

        [Fact]
        public void DestroyChildStore_NonExistingInstance_ReturnsFalse()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;

            // Act
            var destroyed = parentStateStore.DestroyChildStore<int>(999);

            // Assert
            Assert.False(destroyed);
        }

        [Fact]
        public void DestroyChildStore_ThenGetChildStore_CreatesNewStore()
        {
            // Arrange
            var parentStore = new StateStore<string>("parent");
            var parentStateStore = (IStateStore<string>)parentStore;
            var original = parentStateStore.GetChildStore<int>(42);
            parentStateStore.DestroyChildStore<int>(42);

            // Act
            var newStore = parentStateStore.GetChildStore<int>(42);

            // Assert
            Assert.NotSame(original, newStore);
        }

        [Fact]
        public void Keys_ReturnsAllStoredKeys()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("alpha", 1);
            stateStore.Add("beta", 2);

            // Act
            var keys = stateStore.Keys;

            // Assert
            Assert.Contains("alpha", keys);
            Assert.Contains("beta", keys);
            Assert.Equal(2, keys.Count);
        }

        [Fact]
        public void Values_ReturnsAllStoredValues()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("alpha", 100);
            stateStore.Add("beta", 200);

            // Act
            var values = stateStore.Values;

            // Assert
            Assert.Contains(100, values);
            Assert.Contains(200, values);
            Assert.Equal(2, values.Count);
        }

        [Fact]
        public void IsReadOnly_ReturnsFalse()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;

            // Assert
            Assert.False(stateStore.IsReadOnly);
        }

        [Fact]
        public void Contains_KeyValuePair_ReturnsTrueForExisting()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("key", "value");

            // Act
            var contains = stateStore.Contains(new KeyValuePair<string, object>("key", "value"));

            // Assert
            Assert.True(contains);
        }

        [Fact]
        public void GetEnumerator_EnumeratesAllEntries()
        {
            // Arrange
            var store = new StateStore<string>("instance");
            var stateStore = (IDictionary<string, object>)store;
            stateStore.Add("a", 1);
            stateStore.Add("b", 2);
            var count = 0;

            // Act
            foreach (var kvp in stateStore)
            {
                count++;
            }

            // Assert
            Assert.Equal(2, count);
        }
    }
}
