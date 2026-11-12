namespace Units.Domain;

// A domain type of a plugin module. A module handler uses it through the plugin Infrastructure.
public sealed class Meter(int value) {
    public int Value { get; } = value;
}
