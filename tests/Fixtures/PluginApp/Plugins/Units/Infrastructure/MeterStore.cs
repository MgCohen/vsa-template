using Units.Domain;

namespace Units.Infrastructure;

// An infrastructure type of a plugin module, for the handlers of each module.
public sealed class MeterStore {
    readonly Meter meter = new(7);

    public Meter Read() => meter;
}
