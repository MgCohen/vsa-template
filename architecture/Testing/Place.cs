namespace Umpire.Architecture;

public sealed record Place(string Folder, Binding? Binding) {
    public bool IsMapped => Binding is not null;
    public string Part => Binding?.Part.Name ?? "None";
    public string? Path => Binding?.Path;
    public IReadOnlyDictionary<string, string> Values => Binding?.Values ?? new Dictionary<string, string>();
    internal Bindings? Placed { get; init; }
}
