global using NUnit.Framework;
using System.Reflection;
using ArchUnitNET.Fluent;
using VSA.Testing.Architecture;

namespace Rules.Tests;

// The fixture modules in tests/Fixtures/Rules/Modules. Each test gives the rule only the modules it names.
static class Fixtures {
    public static VsaApp App(params string[] modules) => new([.. modules.Select(m => AppModule.Of(Assembly.Load(m)))]);

    public static void Passes(IArchRule rule, VsaApp app) {
        var failed = Failures(rule, app);
        Assert.That(failed, Is.Empty);
    }

    // The text of each failed result.
    public static string Fails(IArchRule rule, VsaApp app) {
        var failed = Failures(rule, app);
        Assert.That(failed, Is.Not.Empty, "The rule passed.");
        return string.Join(Environment.NewLine, failed);
    }

    static List<string> Failures(IArchRule rule, VsaApp app) =>
        [.. rule.Evaluate(VsaArchitectureTests<Fixture>.Load(app)).Where(r => !r.Passed).Select(r => r.Description)];

    // VsaArchitectureTests needs a type argument to call Load. The fixture tests do not use the entry type.
    sealed class Fixture;
}
