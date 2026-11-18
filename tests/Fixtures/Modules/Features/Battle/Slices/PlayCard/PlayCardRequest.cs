namespace Battle.Slices.PlayCard;

using Umpire.Vsa.Contracts.Requests;

public sealed record PlayCardRequest(int Mana) : IRequest<PlayCardResponse>;
