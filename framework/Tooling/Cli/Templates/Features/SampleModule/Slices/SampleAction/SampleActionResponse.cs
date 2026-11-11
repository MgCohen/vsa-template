using VSA.Contracts.Requests;

namespace SampleModule.Slices.SampleAction;

// Add the output of the request as record parameters.
public sealed record SampleActionResponse : IResponse;
