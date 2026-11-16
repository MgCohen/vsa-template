using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;
using Versions.Domain.Shared;

namespace LoadApp.Slices.LoadApp;

public sealed record LoadAppCommand(
    string BlockFolder, string Bin, string Skip, CheckVersionsResult CheckedVersions)
    : EngineStep<AppInput>, IStepGroup {
    public static int Group => 2;
}
