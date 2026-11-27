namespace Spike.Core;

public sealed record MessageResult(object? Value, string? Error) {
    public bool IsSuccess => Error is null;

    public static MessageResult Ok(object? value) => new(value, null);

    public static MessageResult Fail(string error) => new(null, error);
}
