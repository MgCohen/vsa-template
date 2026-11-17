using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using LoadApp.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

namespace LoadApp.Slices.LoadApp;

public sealed record LoadAppCommand(BlockTree Blocks, BinIndex Bin, string Skip) : EngineStep<AppInput>, IStepGroup {
    public static int Group => 3;
}
