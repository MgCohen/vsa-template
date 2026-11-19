namespace Blocks.Domain.Shared;

using BuiltApp.Domain.Shared;

public sealed record BlockTree(BlockEntry Current, IReadOnlyList<BlockEntry> Inner) {
    public IReadOnlyList<string> InnerFolders =>
        [.. Inner.Select(e => e.Folder).Distinct(StringComparer.OrdinalIgnoreCase)];

    public IReadOnlyList<string> Used { get; init; } = [];
}
