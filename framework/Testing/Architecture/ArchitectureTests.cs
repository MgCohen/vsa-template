using ArchUnitNET.Domain;
using NUnit.Framework;
using VSA.Architecture;
using VSA.Architecture.Engine;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VSA.Testing.Architecture;

public abstract class ArchitectureTests : ArchitectureTests<ArchitectureDefinition>;

public abstract class ArchitectureTests<TDefinition> where TDefinition : ArchitectureDefinition {
    static readonly Lazy<ArchitectureMap> LoadedMap = new(() => Load(typeof(TDefinition)));

    protected static ArchitectureMap Map => LoadedMap.Value;

    static ArchitectureMap Load(Type definition) {
        var map = ArchitectureMap.Load(definition);
        foreach (var warning in map.Warnings) {
            TestContext.Progress.WriteLine(warning);
        }
        return map;
    }

    protected static IEnumerable<IType> AppTypes => Map.AppTypes;

    protected static IEnumerable<IType> BoundTypes => Map.BoundTypes;

    protected static IEnumerable<TestCaseData> PartInstances =>
        Map.PartInstances.Select(p => new TestCaseData(p).SetArgDisplayNames(p.Name));

    protected static IEnumerable<TestCaseData> LimitedPartInstances =>
        Map.PartInstances.Where(p => p.OfTypes.Count > 0 && p.Limited.Count > 0)
            .Select(p => new TestCaseData(p).SetArgDisplayNames(p.Name));

    protected static IEnumerable<TestCaseData> ExposureLimitedPartInstances =>
        Map.PartInstances.Where(p => p.Limits.Count > 0)
            .Select(p => new TestCaseData(p).SetArgDisplayNames(p.Name));

    [TestCaseSource(nameof(PartInstances))]
    public void Uses_only_allowed_parts(PartInstance instance) {
        Types().That().AreIn(instance).Should().NotDependOnAnyDeniedTo(instance).Check(Map);
    }

    [TestCaseSource(nameof(LimitedPartInstances))]
    public void Has_only_allowed_types(PartInstance instance) {
        Types().That().Are(instance.Limited).Should().BeAssignableTo(instance.OfTypes).Check(Map);
    }

    [TestCaseSource(nameof(ExposureLimitedPartInstances))]
    public void Exposes_only_allowed_parts(PartInstance instance) {
        Types().That().AreIn(instance).Should().ExposeOnlyWithinLimitsOf(instance).Check(Map);
    }

    [Test]
    public void The_rule_engine_finds_no_errors() {
        var run = RuleEngine.Run();
        if (run is null) {
            Assert.Ignore("The rule engine is not installed. Add the VSA.Architecture.Build package to this test project.");
        }
        Assert.That(run.Errors, Is.Empty, run.Report);
        Assert.That(run.ExitCode, Is.Zero, run.Report);
    }

    [Test]
    public void Each_type_is_in_a_part() {
        Types().That().Are(AppTypes).Should().Be(BoundTypes).Check(Map);
    }
}
