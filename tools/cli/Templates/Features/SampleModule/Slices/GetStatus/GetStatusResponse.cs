namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using Umpire.Vsa.Contracts.Requests;

public sealed record GetStatusResponse(string Module, string Status) : IResponse;
