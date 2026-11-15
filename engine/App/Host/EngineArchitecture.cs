using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace UmpireEngine;

public sealed class EngineArchitecture : VsaArchitecture {
    protected override void Define(Root root) {
        base.Define(root);
        root.Add<EngineLibraries>();
        root.Add<VersionLibraries>();
        root.Add<ArchUnitLibraries>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>()
            .Uses<EngineLibraries>().And<VersionLibraries>();
        rules.For<Infrastructure>().Uses<ArchUnitLibraries>();
        rules.For<Host>().Under<App>().Uses<EngineLibraries>().And<VersionLibraries>();
    }
}
