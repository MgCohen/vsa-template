using Good.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Good.Handlers.Requests;

sealed class GetItemHandler : RoutedHandler<GetItemRequest, GetItemResponse> {
    protected override Task<Result<GetItemResponse>> HandleRequestAsync(GetItemRequest request, CancellationToken ct) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(request.Id));
}
