namespace Cards.Domain.Shared;

// The published domain of Cards. It uses no other module: the resource is a name, not a key of Resources.
public sealed record Cost(string Resource, int Amount);
