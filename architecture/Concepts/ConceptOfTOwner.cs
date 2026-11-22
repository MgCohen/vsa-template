namespace Umpire.Architecture;

public abstract record Concept<TOwner> : Concept, IOwnedBy<TOwner> where TOwner : Architecture;
