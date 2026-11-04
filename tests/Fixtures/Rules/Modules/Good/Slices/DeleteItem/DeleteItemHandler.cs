using VSA.Domain;
using VSA.Handlers.Requests;

namespace Good.Slices.DeleteItem;

sealed class DeleteItemHandler : RoutedHandler<DeleteItemRequest, DeleteItemResponse> {
    protected override Task<Result<DeleteItemResponse>> HandleRequestAsync(DeleteItemRequest request, CancellationToken ct) =>
        Task.FromResult<Result<DeleteItemResponse>>(new DeleteItemResponse(request.Id));
}
