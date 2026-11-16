using Blocks.Domain.Shared;
using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;
using Versions.Domain.Shared;

namespace LoadApp.Slices.LoadApp;

public sealed record LoadAppCommand(
    BlockTree Blocks, string Bin, string Skip, CheckVersionsResult CheckedVersions)
    : EngineStep<AppInput>, IStepGroup {
    public static int Group => 3;
}
