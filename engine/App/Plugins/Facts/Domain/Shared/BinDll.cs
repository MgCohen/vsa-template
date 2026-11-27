namespace Facts.Domain.Shared;

public sealed record BinDll(
    string Path, string Name, bool IsShared, IReadOnlyList<string> References, IReadOnlyList<string> Documents,
    IReadOnlyList<TypeSource> Types, IReadOnlyList<AppClass> AppClasses, bool IsProject = false) {
    public IReadOnlyList<DefinitionType> Definitions { get; init; } = [];

    public IReadOnlyList<UnloadedType> Unloaded { get; init; } = [];

    public static bool IsSystem(string name) =>
        name is "netstandard" or "mscorlib"
        || name.StartsWith("System", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal);

    public bool HasSourceUnder(string blockFolder, IReadOnlyList<string>? ignoredFolders = null) {
        var prefix = blockFolder + System.IO.Path.DirectorySeparatorChar;
        return Documents
            .Where(System.IO.Path.IsPathRooted)
            .Select(System.IO.Path.GetFullPath)
            .Any(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && !ReadOptions.IsIgnored(file[prefix.Length..], ignoredFolders ?? []));
    }
}
