namespace UmpireEngine;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;
using DefinitionRoot = Umpire.Architecture.Blocks.Definition;
using Core = Umpire.Kernels.Blocks.Core;
using KernelParts = Umpire.Kernels.KernelParts;

public sealed class EngineArchitecture : VsaArchitecture {
    protected override void Configure(ArchitectureConfig config) => config.Roots.Add<Entries>();

    protected override void Rules(RuleBook rules) {
        rules.For<Contracts.Commands>().And<Handlers.Commands>().And<Infrastructure>().And<Domain.Shared>()
            .Uses<EngineLibraries>().And<VersionLibraries>().And<ConfigLibraries>();
        rules.For<Infrastructure>().Uses<KernelParts.Utility>().And<KernelParts.Paths>();
        rules.For<Domain>().Uses<KernelParts.Paths>();
        rules.For<Host>().And<App>(Depth.Self).Uses<EngineLibraries>().And<VersionLibraries>();
        rules.For<Host>().Uses<Contracts.Requests>().And<Domain.Shared>().And<Infrastructure>().Under<Plugin>();
        rules.For<DefinitionRoot>().Uses<EngineLibraries>();
        rules.For<Entries>().Uses<App>(Depth.Self).And<Core>().And<UmpireLibraries>()
            .And<EngineLibraries>().And<VersionLibraries>();
        rules.For<Entries>().Uses<Contracts.Requests>().And<Domain.Shared>().And<Infrastructure>().Under<Plugin>();
    }
}
