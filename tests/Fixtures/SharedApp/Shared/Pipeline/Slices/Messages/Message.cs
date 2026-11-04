using RecordIds.Ids;
using Units.Amounts;
using VSA.Contracts.Requests;

namespace Pipeline.Slices.Messages;

// The base of the messages of each module. A message of a module is a request with the response MessageId.
public abstract record Message;

public sealed record MessageId(long Value) : Id(Value), IResponse;

public sealed record Cost(Amount Amount);
