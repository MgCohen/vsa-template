using RecordIds.Contracts.Requests;
using Units.Contracts.Requests;

namespace Pipeline.Contracts.Requests;

// The base of the messages of each module.
public abstract record Message;

public sealed record MessageId(long Value) : Id(Value);

public sealed record Cost(Amount Amount);
