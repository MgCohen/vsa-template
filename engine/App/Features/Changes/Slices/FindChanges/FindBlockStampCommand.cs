namespace Changes.Slices.FindChanges;

using Facts.Domain.Shared;
using Changes.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record FindBlockStampCommand(BlockEntry Block, BinIndex Bin, string Run) : ICommand<BlockStampResult>;
