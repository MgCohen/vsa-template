using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record Binding(
    IType Type, Part Part, IReadOnlyDictionary<string, string> Items, Source Source, bool FollowsUser = false, bool External = false) {
    public string Instance => Items.Aggregate(Part.Path, (path, item) => path.Replace($"{{{item.Key.ToLowerInvariant()}}}", item.Value));
    public string Where => External ? $"{Part.Name} ({Source.File}.dll)" : $"{Part.Name} ({Instance})";
}
