namespace VSA.Handlers;

public sealed class SequentialMessageIds : IMessageIds {
    long last;

    public long Next() => Interlocked.Increment(ref last);
}
