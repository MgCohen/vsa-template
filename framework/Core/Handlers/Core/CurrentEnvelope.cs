using VSA.Contracts;

namespace VSA.Handlers;

static class CurrentEnvelope {
    static readonly AsyncLocal<Envelope?> current = new();

    internal static Envelope? Value {
        get => current.Value;
        set => current.Value = value;
    }
}
