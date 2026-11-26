namespace SpikeApp.Features.Describe.Slices.DescribeType {
    using Umpire.Vsa.Contracts.Requests;

    public sealed record DescribeTypeRequest(string Name, int Members) : IRequest<DescribeTypeResponse>;

    public sealed record DescribeTypeResponse(string Message) : IResponse;
}
