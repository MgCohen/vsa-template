namespace Resources.Domain.Shared;

// The published domain of Resources: other modules read these keys and send commands with them.
public sealed record ResourceKey(string Name);

public static class ResourceKeys {
    public static readonly ResourceKey Mana = new("mana");
}
