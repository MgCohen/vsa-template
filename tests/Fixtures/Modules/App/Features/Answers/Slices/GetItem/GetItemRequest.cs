namespace Answers.Slices.GetItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record GetItemRequest(Guid Id, int Count, bool Loud) : IRequest<GetItemResponse> {
    public static Effect Effect => Effect.Read;
}
