using ArchUnitNET.Domain;
using NUnit.Framework;
using VSA.Architecture;
using VSA.Architecture.Engine;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VSA.Testing.Architecture;

public abstract class ArchitectureTests<TDefinition> where TDefinition : ArchitectureDefinition {
    static readonly Lazy<AppArchitecture> LoadedApp =
        new(() => AppArchitecture.Load(DiscoveredApp.FromTestOutput(typeof(TDefinition))));

    protected static AppArchitecture Architecture => LoadedApp.Value;

    protected static IEnumerable<IType> AppTypes => Architecture.AppTypes;

    protected static IEnumerable<IType> BoundTypes => Architecture.BoundTypes;

    protected static IEnumerable<TestCaseData> PartInstances =>
        Architecture.PartInstances.Select(p => new TestCaseData(p).SetArgDisplayNames(p.Name));

    protected static IEnumerable<TestCaseData> LimitedPartInstances =>
        Architecture.PartInstances.Where(p => p.OfTypes.Count > 0 && p.Limited.Count > 0)
            .Select(p => new TestCaseData(p).SetArgDisplayNames(p.Name));

    [SetUp]
    public void UseThisArchitecture() {
        ArchitectureScope.Current = Architecture;
    }

    [TestCaseSource(nameof(PartInstances))]
    public void Uses_only_allowed_parts(PartInstance instance) {
        Types().That().AreIn(instance).Should().NotDependOnAnyDeniedTo(instance).Check(Architecture);
    }

    [TestCaseSource(nameof(LimitedPartInstances))]
    public void Has_the_type_of_its_part(PartInstance instance) {
        Types().That().Are(instance.Limited).Should().BeAssignableTo(instance.OfTypes).Check(Architecture);
    }

    [Test]
    public void Each_type_is_in_a_part() {
        Types().That().Are(AppTypes).Should().Be(BoundTypes).Check(Architecture);
    }
}
