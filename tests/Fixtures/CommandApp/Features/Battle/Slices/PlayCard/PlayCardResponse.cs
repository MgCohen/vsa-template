using VSA.Contracts.Requests;

namespace Battle.Slices.PlayCard;

public sealed record PlayCardResponse(bool Played, int ManaLeft) : IResponse;
