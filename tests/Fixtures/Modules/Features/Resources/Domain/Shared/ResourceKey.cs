namespace Resources.Domain.Shared;

public sealed record ResourceKey(string Name);

public static class ResourceKeys {
    public static readonly ResourceKey Mana = new("mana");
}
