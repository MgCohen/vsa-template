using System.Reflection;
using VSA.Module;
using VSA.Testing.Architecture;
using VSA.Testing.Core;

namespace Rules.Tests;

// The fixture modules in tests/Fixtures/Rules/Modules. Each test gives the rules only the modules it names.
static class Fixtures {
    public static VsaApp App(params string[] modules) => new([.. modules.Select(Root)]);

    public static ModuleRoot Root(string module) => ModuleRoot.Of(Assembly.Load(module));

    // The message of the failed rule. The rule runs in its own context, so the failure does not fail this test.
    // A rule with more than one check fails with a MultipleAssertException. Both types are a ResultStateException.
    public static string Fails(Action rule) => Assert.Catch<ResultStateException>(rule)!.Message;
}

// The base tests on fixture modules: a fixture test calls a base test, for example
//   FixtureApp.Of("Good").VSA2002_Module_classes_are_public_and_sealed();
// NUnit finds the inherited [Test] methods. Explicit: NUnit does not run them on a fixture app by itself.
[Explicit("A fixture test calls the base tests.")]
sealed class FixtureApp : VsaArchitectureTests {
    public string Module { get; init; } = "";
    public VsaApp? Given { get; init; }
    public IReadOnlyList<Layer>? Map { get; init; }

    protected override VsaApp App => Given ?? Fixtures.App(Module);
    protected override IReadOnlyList<Layer> LayerMap => Map ?? Layers.All;

    public static FixtureApp Of(string module, IReadOnlyList<Layer>? map = null) => new() { Module = module, Map = map };

    // An app of more than one module root, for example a module and the Shared modules that it uses.
    public static FixtureApp Of(VsaApp app) => new() { Given = app };
}
