using Steps.Contracts.Commands;
using Versions.Domain.Shared;

namespace Versions.Slices.CheckVersions;

public sealed record CheckVersionsCommand(string AppRoot, string Bin) : EngineStep<CheckVersionsResult>;
