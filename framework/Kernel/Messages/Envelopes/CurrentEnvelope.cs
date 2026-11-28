namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

static class CurrentEnvelope {
    static readonly AsyncLocal<Envelope?> current = new();

    internal static Envelope? Value {
        get => current.Value;
        set => current.Value = value;
    }
}
