using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;
using Versions.Domain.Shared;

namespace LoadApp.Slices.LoadApp;

public sealed record LoadAppCommand(
    string AppRoot, string Bin, string Skip, CheckVersionsResult CheckedVersions)
    : EngineStep<AppInput>;
