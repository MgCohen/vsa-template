namespace LoadApp.Domain.Shared;

using Blocks.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Architecture;

public sealed record AppInput(
    Architecture Architecture, BlockTree Blocks, IReadOnlyList<string> Dlls, IReadOnlyDictionary<string, Source> Sources) : IStepResult {
    public IReadOnlyList<Finding> Findings { get; init; } = [];
}
