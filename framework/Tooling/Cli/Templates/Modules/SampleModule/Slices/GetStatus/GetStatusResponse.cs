using VSA.Contracts.Requests;

namespace SampleModule.Slices.GetStatus;

public sealed record GetStatusResponse(string Module, string Status) : IResponse;
