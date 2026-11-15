namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using VSA.Contracts.Requests;

public sealed record GetStatusResponse(string Module, string Status) : IResponse;
