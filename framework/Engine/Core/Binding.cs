using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record Binding(
    IType Type, Part Part, IReadOnlyDictionary<string, string> Values, Source Source, bool FollowsUser = false, bool External = false) {
    public string Path => Values.Aggregate(Part.Path, (path, value) => path.Replace($"{{{value.Key.ToLowerInvariant()}}}", value.Value));
    public string Where => External ? $"{Part.Name} ({Source.File}.dll)" : $"{Part.Name} ({Path})";
}
