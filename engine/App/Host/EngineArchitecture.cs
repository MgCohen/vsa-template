namespace UmpireEngine;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;

public sealed class EngineArchitecture : VsaArchitecture {
    protected override void Define(Block block) {
        base.Define(block);
        block.Add<EngineLibraries>();
        block.Add<VersionLibraries>();
        block.Add<ArchUnitLibraries>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>()
            .Uses<EngineLibraries>().And<VersionLibraries>();
        rules.For<Infrastructure>().Uses<ArchUnitLibraries>();
        rules.For<Infrastructure>().Under<Plugin>().Uses<Kernel>(Depth.Self);
        rules.For<Host>().Under<App>().Uses<EngineLibraries>().And<VersionLibraries>();
    }
}
