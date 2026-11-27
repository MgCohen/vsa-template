namespace Facts.Infrastructure;

using Facts.Domain.Shared;

public sealed record BinQuery(string Bin, ReadOptions Options);
