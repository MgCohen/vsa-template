using VSA.Domain;
using VSA.Handlers.Requests;

namespace Answers.Slices.GetItem;

sealed class GetItemHandler : RequestHandler<GetItemRequest, GetItemResponse> {
    protected override Task<Result<GetItemResponse>> HandleRequestAsync(GetItemRequest request, CancellationToken ct) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(request.Id, request.Count, request.Loud));
}
