using VSA.Contracts.Requests;

namespace SampleModule.Contracts.Requests.GetStatus;

public sealed record GetStatusResponse(string Module, string Status) : IResponse;
