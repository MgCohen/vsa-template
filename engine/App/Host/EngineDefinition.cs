using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace VsaEngine;

public sealed class EngineDefinition : VsaDefinition {
    protected override void Define(Root root) {
        base.Define(root);
        root.Add<EngineLibraries>();
        root.Add<CheckerLibraries>();
        root.Add<ArchUnitLibraries>();
        root.Add<DataLibraries>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>().Uses<EngineLibraries>();
        rules.For<Infrastructure>().Uses<CheckerLibraries>().And<ArchUnitLibraries>();
        rules.For<Handlers.Commands>().Uses<DataLibraries>();
        rules.For<Host>().Under<App>().Uses<EngineLibraries>();
    }
}
