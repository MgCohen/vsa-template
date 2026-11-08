using VSA.Domain;
using VSA.Handlers.Requests;

namespace Answers.Slices.ListAll;

sealed class ListAllHandler : RequestHandler<ListAllRequest, ListAllResponse> {
    protected override Task<Result<ListAllResponse>> HandleRequestAsync(ListAllRequest request, CancellationToken ct) =>
        Task.FromResult<Result<ListAllResponse>>(new ListAllResponse(3));
}
