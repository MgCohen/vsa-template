using System.Reflection;
using VSA.Architecture.Engine;
using VSA.Testing.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Architecture.Tests;

public sealed class TestBaseTests : ArchitectureTestBase<GoodDefinition> {
    [Test]
    public void A_custom_test_uses_the_map_of_the_base() {
        Types().That().AreIn<Domain>(Map).Should().NotDependOnAny<Presenters>(Map).Check(Map);
    }

    [Test]
    public void The_base_and_the_tests_share_one_loaded_map() {
        Assert.That(Map, Is.SameAs(ArchitectureMap.Load(typeof(GoodDefinition))));
    }

    [Test]
    public void The_base_has_no_tests_of_its_own() {
        var tests = typeof(ArchitectureTestBase<>).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.GetCustomAttributes().Any(a => a is TestAttribute or TestCaseSourceAttribute));

        Assert.That(tests, Is.Empty);
    }
}
