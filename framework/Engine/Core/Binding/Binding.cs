using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record Binding(
    IType Type, Part Part, IReadOnlyDictionary<string, string> Values, Source? Source, bool FollowsUser = false) {
    public bool External => Part.IsExternal;
    public string Path => Values.Aggregate(Part.Path, (path, value) => path.Replace($"{{{value.Key.ToLowerInvariant()}}}", value.Value));
    public string Where => External ? $"{Part.Name} ({Type.Assembly.Name.Split(',')[0]}.dll)" : $"{Part.Name} ({Path})";
}
