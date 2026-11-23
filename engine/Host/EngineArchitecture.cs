namespace UmpireEngine;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;
using KernelParts = Umpire.Kernels.KernelParts;

public sealed class EngineArchitecture : VsaArchitecture {
    protected override void Rules(RuleBook rules) {
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>()
            .Uses<EngineLibraries>().And<VersionLibraries>().And<ConfigLibraries>();
        rules.For<Infrastructure>().Uses<ArchUnitLibraries>();
        rules.For<Infrastructure>().Under<Plugin>().Uses<KernelParts.App>();
        rules.For<Host>().Uses<EngineLibraries>().And<VersionLibraries>();
    }
}
