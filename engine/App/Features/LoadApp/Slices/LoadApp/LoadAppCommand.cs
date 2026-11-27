using Blocks.Domain.Shared;
using Facts.Domain.Shared;
using LoadApp.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

namespace LoadApp.Slices.LoadApp;

public sealed record LoadAppCommand(BlockTree Blocks, BinIndex Bin, string Skip, bool IsUsed = false)
    : JobOperation<AppInput>, IOperationGroup {
    public static int Group => 5;
}
