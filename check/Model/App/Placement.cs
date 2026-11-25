namespace Umpire.CheckModel;

public sealed class Placement {
    public Placement(Part part, IReadOnlyDictionary<string, string> values, string assembly, bool isFromDll = false, bool followsUser = false) {
        Part = part;
        Values = values;
        IsExternal = part.IsExternal || isFromDll;
        FollowsUser = followsUser;
        Path = InstanceOf(part);
        Where = IsExternal ? $"{part.Name} ({assembly}.dll)" : $"{part.Name} ({Path})";
        Assembly = assembly;
    }

    public Part Part { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
    public string Assembly { get; }
    public bool IsExternal { get; }
    public bool FollowsUser { get; }
    public string Path { get; }
    public string Where { get; }

    public string? Value(Part group) => Values.TryGetValue(group.Name, out var value) ? value : null;

    public string InstanceOf(Part group) =>
        Values.Aggregate(group.Path, (path, value) => path.Replace($"{{{value.Key.ToLowerInvariant()}}}", value.Value));

    public bool IsSameInstance(Placement other) => Part == other.Part && Path == other.Path;

    internal Placement Follow(Part user) => new(user, Values, Assembly, IsExternal, followsUser: true);
}
