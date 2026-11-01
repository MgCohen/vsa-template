namespace VSA.Presenters;

// A request that the handler refused. Code and Message come from the Error of the handler.
public sealed class RequestException(string code, string message) : Exception(message) {
    public string Code { get; } = code;
}
