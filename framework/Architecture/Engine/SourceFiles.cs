namespace VSA.Architecture.Engine;

public static class SourceFiles {
    public static Dictionary<string, Source> Read(IEnumerable<string> dlls) {
        var result = new Dictionary<string, Source>();
        foreach (var dll in dlls.Where(HasSymbols)) {
            using var symbols = new SymbolReader(dll);
            foreach (var type in symbols.TypeSources()) {
                result.TryAdd(type.FullName, type.Source);
            }
        }
        return result;
    }

    public static bool HasSymbols(string dll) => File.Exists(Path.ChangeExtension(dll, ".pdb"));

    public static IReadOnlyList<string> Documents(string dll) {
        if (!HasSymbols(dll)) {
            return [];
        }
        try {
            using var symbols = new SymbolReader(dll);
            return symbols.Documents();
        } catch (Exception e) when (e is BadImageFormatException or IOException) {
            return [];
        }
    }

    public static bool HasSourceUnder(string dll, string appRoot) {
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(appRoot)) + Path.DirectorySeparatorChar;
        return Documents(dll)
            .Where(Path.IsPathRooted)
            .Select(Path.GetFullPath)
            .Any(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !UnityProject.IsInLibrary(file[prefix.Length..]));
    }
}
