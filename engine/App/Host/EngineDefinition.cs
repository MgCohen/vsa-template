using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace VsaEngine;

public sealed class EngineDefinition : VsaDefinition {
    protected override void Define(Root root) {
        base.Define(root);
        root.Add<EngineLibraries>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Contracts.Commands>().And<Handlers.Commands>().Uses<EngineLibraries>();
        rules.For<Host>().Under<App>().Uses<EngineLibraries>();
    }
}
