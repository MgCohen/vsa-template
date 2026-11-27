namespace Umpire.Cli;

using Umpire.Architecture;
using Umpire.Vsa.Definition;
using MessagesParts = Umpire.Kernels.MessagesParts;

public sealed class ToolArchitecture : VsaArchitecture {
    protected override void Rules(RuleBook rules) {
        rules.For<Domain>().And<Handlers.Requests>()
            .Uses<FilesLibraries>().And<VersionLibraries>().And<ConfigLibraries>();
        rules.For<Host>().Uses<Contracts.Requests>().And<Domain.Shared>().Under<Plugin>();
        rules.For<Host>().Uses<MessagesParts.Contracts>();
    }
}
