namespace UmpireEngine;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;

public sealed class EngineArchitecture : VsaArchitecture {
    protected override void Define(Part root) {
        base.Define(root);
        root.Add<EngineLibraries>();
        root.Add<VersionLibraries>();
        root.Add<ArchUnitLibraries>();
        root.Add<ConfigLibraries>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>()
            .Uses<EngineLibraries>().And<VersionLibraries>().And<ConfigLibraries>();
        rules.For<Infrastructure>().Uses<ArchUnitLibraries>();
        rules.For<Infrastructure>().Under<Plugin>().Uses<Kernel>(Depth.Self);
        rules.For<Host>().Under<App>().Uses<EngineLibraries>().And<VersionLibraries>();
    }
}
