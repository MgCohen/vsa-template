namespace Scaffold.Infrastructure;

using Scaffold.Domain.Shared;
using Umpire.Architecture;

public static class Planner {
    const string LineEnd = "\n";
    const string WindowsLineEnd = "\r\n";
    const string CodeFiles = "*.cs";

    public static Planned Plan(TemplateSet set, ScaffoldInput input, Bindings app, string appBlock) {
        var tokens = new TokenMap().Add(set.Token, input.Name);
        GroupFolder? parent = null;
        foreach (var group in ParentGroups(set)) {
            if (!input.Groups.TryGetValue(group, out var value)) {
                return new ScaffoldProblem(ScaffoldProblemKind.MissingGroupValue,
                    $"The {input.Kind} needs the {group} that holds it. Give it as {group}=<Name>.");
            }
            if (GroupFolder.Find(app, group, value) is not { } found) {
                return new ScaffoldProblem(ScaffoldProblemKind.NoGroupFolder,
                    $"The {group} {value} does not exist in the app. "
                    + $"Add it first, or give the name of an existing {group}.");
            }
            tokens.Add(set.First.Values[group], found.Value);
            parent = Deeper(set, parent, found);
        }
        tokens.Add(set.First.Type.Assembly, parent?.Assembly ?? set.First.Type.Assembly);
        var root = parent is null ? Path.GetFullPath(appBlock) : parent.Folder;
        var folder = Relative(appBlock, Path.Combine(root, Remainder(set, parent), input.Name));
        IReadOnlyList<PlannedFile> files = [.. Directory.GetFiles(set.Folder, CodeFiles)
            .Order(StringComparer.Ordinal)
            .Select(file => new PlannedFile(
                $"{folder}/{tokens.Apply(Path.GetFileName(file))}", Content(file, tokens)))];
        return new ScaffoldPlan(folder, files);
    }

    static string Content(string file, TokenMap tokens) =>
        tokens.Apply(File.ReadAllText(file).Replace(WindowsLineEnd, LineEnd, StringComparison.Ordinal));

    static IEnumerable<string> ParentGroups(TemplateSet set) =>
        set.First.Values.Keys.Where(group => group != set.Group).OrderBy(group => Position(set, group));

    static GroupFolder? Deeper(TemplateSet set, GroupFolder? current, GroupFolder found) =>
        current is null || Position(set, found.Group) > Position(set, current.Group) ? found : current;

    static int Position(TemplateSet set, string group) =>
        set.First.Part.Path.IndexOf(GroupFolder.Placeholder(group), StringComparison.Ordinal);

    static string Remainder(TemplateSet set, GroupFolder? parent) {
        var path = set.First.Part.Path;
        var start = parent is null ? 0 : Position(set, parent.Group) + GroupFolder.Placeholder(parent.Group).Length + 1;
        var own = path.IndexOf(GroupFolder.Placeholder(set.Group), StringComparison.Ordinal);
        return path[start..own];
    }

    static string Relative(string block, string folder) =>
        Path.GetRelativePath(Path.GetFullPath(block), Path.GetFullPath(folder))
            .Replace(Path.DirectorySeparatorChar, '/');
}
