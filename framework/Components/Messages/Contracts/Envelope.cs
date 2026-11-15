namespace Umpire.Messages.Contracts;

public abstract class Envelope : IEnvelope {
    static long lastId;

    private protected Envelope(Envelope? parent, IServiceProvider services, CancellationToken cancellation) {
        Id = Interlocked.Increment(ref lastId);
        Parent = parent;
        Depth = parent is null ? 0 : parent.Depth + 1;
        Services = services;
        Cancellation = cancellation;
    }

    public long Id { get; }
    public Envelope? Parent { get; }
    public int Depth { get; }
    public EnvelopeState State { get; internal set; }
    public EnvelopeStage Stage { get; internal set; }
    public CancellationToken Cancellation { get; }
    public IServiceProvider Services { get; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
    public Error? Error { get; private set; }
    public IMessageResult? Result { get; private set; }
    public Exception? Exception { get; private set; }
    public object Message => UntypedMessage;
    protected abstract object UntypedMessage { get; }

    public static Envelope<TMessage> For<TMessage>(
        TMessage message, IServiceProvider services, CancellationToken cancellation = default)
        where TMessage : class =>
        new(message, null, services, cancellation);

    public void Reject(Error error) {
        if (Stage > EnvelopeStage.BeforeHooks) {
            throw new InvalidOperationException(
                $"Envelope {Id} is in stage {Stage}. Reject it in a validator, a before step or a Before hook.");
        }
        Error = error ?? throw new ArgumentNullException(nameof(error));
        State = EnvelopeState.Rejected;
    }

    internal void Accept(IMessageResult result) {
        Result = result;
        Error = result.Error;
        State = EnvelopeState.Accepted;
    }

    internal void Fault(Exception exception) {
        Exception = exception;
        State = EnvelopeState.Faulted;
    }

    internal abstract void Replace(object message);
}

public sealed class Envelope<TMessage> : Envelope, IEnvelope<TMessage> where TMessage : class {
    internal Envelope(
        TMessage message, Envelope? parent, IServiceProvider services, CancellationToken cancellation)
        : base(parent, services, cancellation) {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Original = message;
    }

    public new TMessage Message { get; private set; }
    public TMessage Original { get; }
    protected override object UntypedMessage => Message;

    internal override void Replace(object message) =>
        Message = message as TMessage ?? throw new InvalidOperationException(
            $"A before step or a Before hook returned {message?.GetType().Name ?? "null"} "
            + $"for {typeof(TMessage).Name}. "
            + $"Return a {typeof(TMessage).Name}: the same message, or a copy made with 'with'.");
}
