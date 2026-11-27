namespace Facts.Domain.Shared;

public sealed record UnloadedType(TypeName Type, string Source, string? MissingType, string MissingAssembly) {
    public string? Folder { get; init; } = DefinitionType.BlockFolder(Source);

    public UnloadedType In(Func<string, string> folderOf, Func<string, string?> blockOfAliasedSource) =>
        this with { Folder = Folder is { } folder ? folderOf(folder) : blockOfAliasedSource(Source) };

    public override string ToString() => MissingType is { } missing
        ? $"{Type.FullName}: the type {missing} did not load. "
            + $"Its assembly {MissingAssembly} is not in --bin or --references, or it has another version."
        : $"{Type.FullName}: the assembly {MissingAssembly} is not in --bin or --references.";
}
