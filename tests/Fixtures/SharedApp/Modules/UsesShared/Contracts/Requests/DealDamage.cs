using Pipeline.Contracts.Requests;

namespace UsesShared.Contracts.Requests;

// A message of the module, on the Shared base of its layer.
public sealed record DealDamage(int Amount) : Message;
