namespace Blocks.Infrastructure;

using BuiltApp.Domain.Shared;

public sealed record EntryFile(BlockEntry Entry, string? Problem);
