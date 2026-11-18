namespace Good.Slices.GetItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record GetItemRequest(Guid Id) : IRequest<GetItemResponse> {
    public static Effect Effect => Effect.Read;
}

public sealed record GetItemResponse(Guid Id) : IResponse;
