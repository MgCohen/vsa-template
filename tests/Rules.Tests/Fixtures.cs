global using Xunit;
using System.Reflection;
using VSA.Testing;

namespace Rules.Tests;

// The fixture modules in tests/Fixtures/Rules/Modules. Each test gives the rule only the modules it names.
static class Fixtures {
    public static VsaApp App(params string[] modules) => new([.. modules.Select(m => AppModule.FromRoot(Assembly.Load(m)))]);

    public static string Fails(IVsaRule rule, VsaApp app) =>
        Assert.Throws<ArchitectureRuleException>(() => VsaRules.Assert(rule, app)).Message;
}
