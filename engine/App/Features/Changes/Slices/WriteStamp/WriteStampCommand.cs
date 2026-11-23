namespace Changes.Slices.WriteStamp;

using Changes.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record WriteStampCommand(string Bin, string Hash) : ICommand<StampResult>;
