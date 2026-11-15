using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

static class CurrentEnvelope {
    static readonly AsyncLocal<Envelope?> current = new();

    internal static Envelope? Value {
        get => current.Value;
        set => current.Value = value;
    }
}
