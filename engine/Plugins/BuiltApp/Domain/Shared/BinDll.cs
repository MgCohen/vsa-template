namespace BuiltApp.Domain.Shared;

public sealed record BinDll(
    string Path, string Name, bool IsShared, IReadOnlyList<string> References, IReadOnlyList<string> Documents,
    IReadOnlyList<TypeSource> Types, IReadOnlyList<AppClass> AppClasses) {
    const string UnityLibrary = "Library/";

    public static bool IsSystem(string name) =>
        name is "netstandard" or "mscorlib"
        || name.StartsWith("System", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal);

    public bool HasSourceUnder(string blockFolder) {
        var prefix = blockFolder + System.IO.Path.DirectorySeparatorChar;
        return Documents
            .Where(System.IO.Path.IsPathRooted)
            .Select(System.IO.Path.GetFullPath)
            .Any(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !IsInUnityLibrary(file[prefix.Length..]));
    }

    static bool IsInUnityLibrary(string relative) =>
        relative.Replace('\\', '/').StartsWith(UnityLibrary, StringComparison.OrdinalIgnoreCase);
}
