using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Good.Slices.DeleteItem;

sealed class DeleteItemHandler : RequestHandler<DeleteItemRequest, DeleteItemResponse> {
    public override Task<Result<DeleteItemResponse>> HandleAsync(Envelope<DeleteItemRequest> envelope) =>
        Task.FromResult<Result<DeleteItemResponse>>(new DeleteItemResponse(envelope.Message.Id));
}
