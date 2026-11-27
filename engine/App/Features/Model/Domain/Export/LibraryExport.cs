namespace Umpire.CheckModel.Export;

public static class LibraryExport {
    public static Snapshot Of(string assembly, FileMark? source, IEnumerable<LibraryType> types) =>
        Snapshot.Of(assembly, types.GroupBy(t => t.File.Replace('\\', '/')).Select(file =>
            new FileFacts(file.Key, "", 0, false, [.. file.Select(type => Facts(new TypeName(type.FullName, assembly), type))])))
            with { Source = source };

    public static Snapshot Of(Snapshot project, FileMark? source, Func<string, string>? pathOf = null) {
        var loaded = LoadedSnapshot.Of("", project);
        var types = project.Files.SelectMany(file => file.Types.Select(type => new LibraryType(
            type.Name.FullName, (pathOf ?? Same)(loaded.FileOf(file)), type.Location.Line, type.Location.Column,
            [.. type.Content.Bases.Where(b => b != type.Name)])));
        return Of(project.Project, source, types);
    }

    static string Same(string path) => path;

    static TypeFacts Facts(TypeName name, LibraryType type) => new(
        name,
        new TypeLocation(name.Namespace, type.Line, type.Column, DeclaringType(name)),
        new TypeContent(
            TypeKind.Class, MemberAccess.Public, false, false, false, false, false, false, false, [], [], [],
            [.. type.Bases.Prepend(name).Distinct()], [], [], [], []),
        []);

    static string? DeclaringType(TypeName name) {
        var plus = name.FullName.LastIndexOf('+');
        return plus < 0 ? null : name.FullName.Substring(0, plus);
    }
}
