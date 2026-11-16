namespace Good.Slices.GetItem;

using Good.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class GetItemHandler : RequestHandler<GetItemRequest, GetItemResponse> {
    public override Task<Result<GetItemResponse>> HandleAsync(Envelope<GetItemRequest> envelope) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(new Item(envelope.Message.Id).Id));
}
