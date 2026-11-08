namespace VSA.Domain;

public readonly record struct Result<T> {
    Result(T value, Error? error) {
        Value = value;
        Error = error;
    }

    public T Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(Error error) => new(default!, error);
}
