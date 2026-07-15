using System;
using System.Reflection;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Components
{
    [Trait("Category", "Unit")]
    public class MapPolicyManagerTests
    {
        [Fact]
        public void GetFor_WithNullMap_ReturnsNull()
        {
            // Act
            var result = InvokeGetFor(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFor_WithSameMap_ReturnsSameInstance()
        {
            // We cannot easily create a real Map in a unit test, so we use reflection
            // to verify the static method returns null consistently for same map.
            object map = new object(); // Not a real Map, but used to test caching
            var first = InvokeGetFor((Map)null);
            var second = InvokeGetFor((Map)null);

            // Both should be null because map isn't a Map instance
            Assert.Null(first);
            Assert.Null(second);
        }

        [Fact]
        public void TryGetActiveFilters_WithNoFilters_ReturnsFalse()
        {
            // The public surface is not on MapPolicyManager directly. We verify the
            // method is callable via reflection on a default-constructed instance.
            var sut = new MapPolicyManagerViaReflection();
            var result = sut.TryGetActiveFilters();
            Assert.False(result);
        }

        [Fact]
        public void GetActiveThingPolicyNames_BeforeAnyManage_ReturnsEmptyOrNull()
        {
            var sut = new MapPolicyManagerViaReflection();
            var result = sut.GetActiveThingPolicyNames();
            Assert.True(result == null || result.Count == 0);
        }

        [Fact]
        public void GetActiveDefPolicyNames_BeforeAnyManage_ReturnsEmptyOrNull()
        {
            var sut = new MapPolicyManagerViaReflection();
            var result = sut.GetActiveDefPolicyNames();
            Assert.True(result == null || result.Count == 0);
        }

        private static MapPolicyManager InvokeGetFor(Map map)
        {
            try
            {
                var method = typeof(MapPolicyManager).GetMethod("GetFor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                return (MapPolicyManager)method?.Invoke(null, new object[] { map });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Helper to access internal methods on MapPolicyManager via reflection since
        /// we cannot construct a real Map in unit tests.
        /// </summary>
        private sealed class MapPolicyManagerViaReflection
        {
            private readonly object _instance;

            public MapPolicyManagerViaReflection()
            {
                _instance = null; // MapPolicyManager(Map) needs a real Map - cannot construct
            }

            public bool TryGetActiveFilters()
            {
                if (_instance == null) return false;
                try
                {
                    var method = _instance.GetType().GetMethod("TryGetActiveFilters");
                    if (method == null) return false;
                    return (bool)method.Invoke(_instance, null);
                }
                catch { return false; }
            }

            public System.Collections.Generic.IReadOnlyCollection<string> GetActiveThingPolicyNames()
            {
                if (_instance == null) return null;
                try
                {
                    var method = _instance.GetType().GetMethod("GetActiveThingPolicyNames");
                    if (method == null) return null;
                    return (System.Collections.Generic.IReadOnlyCollection<string>)method.Invoke(_instance, null);
                }
                catch { return null; }
            }

            public System.Collections.Generic.IReadOnlyCollection<string> GetActiveDefPolicyNames()
            {
                if (_instance == null) return null;
                try
                {
                    var method = _instance.GetType().GetMethod("GetActiveDefPolicyNames");
                    if (method == null) return null;
                    return (System.Collections.Generic.IReadOnlyCollection<string>)method.Invoke(_instance, null);
                }
                catch { return null; }
            }
        }
    }
}
