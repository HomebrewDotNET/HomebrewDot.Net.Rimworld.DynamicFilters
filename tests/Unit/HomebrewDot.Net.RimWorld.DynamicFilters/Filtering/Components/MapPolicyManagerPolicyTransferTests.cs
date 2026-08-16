using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Filtering.Components;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Filtering.Components
{
    /// <summary>
    /// Tests for the policy transfer support used to keep dynamic policies in sync when Better Workbench
    /// Management copies or links bills. The storage maps are injected directly onto an uninitialized
    /// manager so the lookup logic can be verified without constructing a real <see cref="Verse.Map"/>.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MapPolicyManagerPolicyTransferTests
    {
        private const string DefMapField = "_storageToDefFilterMap";
        private const string InvertedDefMapField = "_storageToInvertedDefFilterMap";
        private const string ThingMapField = "_storageToThingFilterMap";
        private const string InvertedThingMapField = "_storageToInvertedThingFilterMap";

        [Fact]
        public void TryGetPolicyForStorage_WithDefPolicy_ReturnsNameAndInversion()
        {
            var sut = CreateUninitializedManager();
            SetMap(sut, DefMapField, "source", "policyA");
            SetMap(sut, InvertedDefMapField, "source", "policyA");

            var result = InvokeTryGetPolicyForStorage(sut, "source", false, out var policyName, out var inverted);

            Assert.True(result);
            Assert.Equal("policyA", policyName);
            Assert.True(inverted);
        }

        [Fact]
        public void TryGetPolicyForStorage_WithThingPolicy_ReturnsNameWithoutInversion()
        {
            var sut = CreateUninitializedManager();
            SetMap(sut, ThingMapField, "source", "policyB");

            var result = InvokeTryGetPolicyForStorage(sut, "source", true, out var policyName, out var inverted);

            Assert.True(result);
            Assert.Equal("policyB", policyName);
            Assert.False(inverted);
        }

        [Fact]
        public void TryGetPolicyForStorage_ThingLookupDoesNotReadDefMap()
        {
            var sut = CreateUninitializedManager();
            SetMap(sut, DefMapField, "source", "policyA");

            var result = InvokeTryGetPolicyForStorage(sut, "source", true, out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryGetPolicyForStorage_WithoutPolicy_ReturnsFalse()
        {
            var sut = CreateUninitializedManager();

            var result = InvokeTryGetPolicyForStorage(sut, "missing", false, out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryGetPolicyForStorage_WithNullOrEmptyStorageId_ReturnsFalse()
        {
            var sut = CreateUninitializedManager();

            Assert.False(InvokeTryGetPolicyForStorage(sut, null, false, out _, out _));
            Assert.False(InvokeTryGetPolicyForStorage(sut, string.Empty, false, out _, out _));
            Assert.False(InvokeTryGetPolicyForStorage(sut, "   ", false, out _, out _));
        }

        private static MapPolicyManager CreateUninitializedManager()
        {
            var manager = (MapPolicyManager)FormatterServices.GetUninitializedObject(typeof(MapPolicyManager));
            // The uninitialized object skips field initializers, so seed the storage maps directly.
            SetMap(manager, DefMapField, null, null);
            SetMap(manager, InvertedDefMapField, null, null);
            SetMap(manager, ThingMapField, null, null);
            SetMap(manager, InvertedThingMapField, null, null);
            return manager;
        }

        private static void SetMap(MapPolicyManager manager, string fieldName, string storageId, string policyName)
        {
            var field = typeof(MapPolicyManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            var map = (Dictionary<string, string>)(field.GetValue(manager) ?? new Dictionary<string, string>());
            if (storageId != null)
            {
                map[storageId] = policyName;
            }
            field.SetValue(manager, map);
        }

        private static bool InvokeTryGetPolicyForStorage(MapPolicyManager manager, string storageId, bool isForThing, out string policyName, out bool inverted)
        {
            var method = typeof(MapPolicyManager).GetMethod("TryGetPolicyForStorage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method);
            var args = new object[] { storageId, isForThing, null, false };
            var result = (bool)method.Invoke(manager, args);
            policyName = (string)args[2];
            inverted = (bool)args[3];
            return result;
        }
    }
}
