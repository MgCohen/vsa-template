namespace Spike.Core;

public sealed record Operation(string Name, IReadOnlyDictionary<string, string> Values) {
    public override string ToString() => $"Operation {Name} {string.Join(" ", Values.Select(v => $"{v.Key}={v.Value}"))}";
}
