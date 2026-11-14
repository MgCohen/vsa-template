namespace VSA.Contracts;

public interface IEnvelope {
    long Id { get; }
    Envelope? Parent { get; }
    int Depth { get; }
    EnvelopeState State { get; }
    EnvelopeStage Stage { get; }
    CancellationToken Cancellation { get; }
    IServiceProvider Services { get; }
    IDictionary<string, object?> Items { get; }
    Error? Error { get; }
    IMessageResult? Result { get; }

    void Reject(Error error);
}

public interface IEnvelope<out TMessage> : IEnvelope where TMessage : class {
    TMessage Message { get; }
    TMessage Original { get; }
}
