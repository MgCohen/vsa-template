namespace Versions.Slices.CheckVersions;

using Steps.Contracts.Commands;
using Versions.Domain.Shared;

public sealed record CheckVersionsCommand(string AppRoot, string Bin) : EngineStep<CheckVersionsResult>;
