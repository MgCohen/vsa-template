using Good.Domain;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Good.Slices.GetItem;

sealed class GetItemHandler : RequestHandler<GetItemRequest, GetItemResponse> {
    public override Task<Result<GetItemResponse>> HandleAsync(Envelope<GetItemRequest> envelope) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(new Item(envelope.Message.Id).Id));
}
