namespace VSA.Contracts;

public readonly record struct Result<T> : IMessageResult {
    Result(T value, Error? error, Envelope? envelope) {
        Value = value;
        Error = error;
        Envelope = envelope;
    }

    public T Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public Envelope? Envelope { get; }
    object? IMessageResult.Value => Value;

    public static implicit operator Result<T>(T value) => new(value, null, null);
    public static implicit operator Result<T>(Error error) => new(default!, error, null);

    internal Result<T> On(Envelope envelope) => new(Value, Error, envelope);
}
