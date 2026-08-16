using System;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.DynamicFilters.Tests.Comparing.Components
{
    /// <summary>
    /// Tests for the <see cref="MatchesThingFilterOperatorType"/> operator, which checks the left thing against the
    /// right <see cref="SpecialThingFilterDef"/> by delegating to the def's worker — the exact check the vanilla
    /// stockpile UI performs for special thing filters.
    /// </summary>
    [Trait("Category", "Unit")]
    public class MatchesThingFilterOperatorTypeTests
    {
        [Fact]
        public void Compare_WorkerMatches_ReturnsTrue()
        {
            var def = MakeDef(typeof(AlwaysTrueWorker));
            var thing = MakeUninitializedThing();

            Assert.True(MatchesThingFilterOperatorType.Instance.Compare(thing, def, null, null));
        }

        [Fact]
        public void Compare_WorkerDoesNotMatch_ReturnsFalse()
        {
            var def = MakeDef(typeof(AlwaysFalseWorker));
            var thing = MakeUninitializedThing();

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare(thing, def, null, null));
        }

        [Fact]
        public void Compare_NullLeft_ReturnsFalse()
        {
            var def = MakeDef(typeof(AlwaysTrueWorker));

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare(null, def, null, null));
        }

        [Fact]
        public void Compare_NullRight_ReturnsFalse()
        {
            var thing = MakeUninitializedThing();

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare(thing, null, null, null));
        }

        [Fact]
        public void Compare_LeftNotAThing_ReturnsFalse()
        {
            var def = MakeDef(typeof(AlwaysTrueWorker));

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare("not a thing", def, null, null));
        }

        [Fact]
        public void Compare_RightNotASpecialThingFilterDef_ReturnsFalse()
        {
            var thing = MakeUninitializedThing();

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare(thing, "not a def", null, null));
        }

        [Fact]
        public void Compare_DefWithoutWorkerClass_ReturnsFalse()
        {
            var def = MakeDef(null);
            var thing = MakeUninitializedThing();

            Assert.False(MatchesThingFilterOperatorType.Instance.Compare(thing, def, null, null));
        }

        // ── Helpers ──

        private static SpecialThingFilterDef MakeDef(Type workerClass)
        {
            var def = (SpecialThingFilterDef)FormatterServices.GetUninitializedObject(typeof(SpecialThingFilterDef));
            def.defName = "TestFilter";
            def.workerClass = workerClass;
            return def;
        }

        private static Thing MakeUninitializedThing()
        {
            return (Thing)FormatterServices.GetUninitializedObject(typeof(Thing));
        }

        public sealed class AlwaysTrueWorker : SpecialThingFilterWorker
        {
            public override bool Matches(Thing t)
            {
                return true;
            }
        }

        public sealed class AlwaysFalseWorker : SpecialThingFilterWorker
        {
            public override bool Matches(Thing t)
            {
                return false;
            }
        }
    }
}
