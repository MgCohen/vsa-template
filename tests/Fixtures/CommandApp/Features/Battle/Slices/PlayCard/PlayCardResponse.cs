namespace Battle.Slices.PlayCard;

using Umpire.Vsa.Contracts.Requests;

public sealed record PlayCardResponse(bool Played, int ManaLeft) : IResponse;
