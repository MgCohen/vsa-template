namespace SpikeApp.Features.Describe.Slices.DescribeType {
    using Umpire.Messages.Contracts;
    using Umpire.Vsa.Handlers.Requests;

    public sealed class DescribeTypeHandler : RequestHandler<DescribeTypeRequest, DescribeTypeResponse> {
        public override Task<Result<DescribeTypeResponse>> HandleAsync(IEnvelope<DescribeTypeRequest> envelope) =>
            Task.FromResult<Result<DescribeTypeResponse>>(
                new DescribeTypeResponse($"Engine says: type {envelope.Message.Name} has {envelope.Message.Members} members."));
    }
}
