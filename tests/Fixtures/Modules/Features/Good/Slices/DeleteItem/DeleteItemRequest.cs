namespace Good.Slices.DeleteItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record DeleteItemRequest(Guid Id) : IRequest<DeleteItemResponse> {
    public static Effect Effect => Effect.Delete;
}

public sealed record DeleteItemResponse(Guid Id) : IResponse;
