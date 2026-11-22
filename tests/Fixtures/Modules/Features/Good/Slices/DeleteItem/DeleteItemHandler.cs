namespace Good.Slices.DeleteItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class DeleteItemHandler : RequestHandler<DeleteItemRequest, DeleteItemResponse> {
    public override Task<Result<DeleteItemResponse>> HandleAsync(IEnvelope<DeleteItemRequest> envelope) =>
        Task.FromResult<Result<DeleteItemResponse>>(new DeleteItemResponse(envelope.Message.Id));
}
