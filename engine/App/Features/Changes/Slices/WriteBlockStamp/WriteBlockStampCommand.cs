namespace Changes.Slices.WriteBlockStamp;

using Changes.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record WriteBlockStampCommand(string File, string Hash) : ICommand<StampResult>;
