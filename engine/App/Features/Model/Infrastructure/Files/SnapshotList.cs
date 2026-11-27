namespace Umpire.CheckModel.Export;

public static class SnapshotList {
    const char Separator = '|';
    const char Lines = ';';
    const int Fields = 3;

    public static IReadOnlyList<SnapshotPlace> Read(string? file) {
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) {
            return [];
        }
        return [.. File.ReadAllLines(file!)
            .SelectMany(line => line.Split(Lines))
            .Select(line => line.Trim().Split(Separator))
            .Where(parts => parts.Length == Fields && parts[0].Length > 0 && parts[2].Length > 0)
            .Select(parts => new SnapshotPlace(parts[0], BlockPath.Full(parts[2]), parts[1].Length > 0 ? parts[1] : null))
            .GroupBy(place => place.File, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())];
    }

    public static string Line(SnapshotPlace place) => string.Join(Separator.ToString(), place.Project, place.Target ?? "", place.File);
}
