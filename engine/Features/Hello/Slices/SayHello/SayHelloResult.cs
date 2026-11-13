using Steps.Domain.Shared;

namespace Hello.Slices.SayHello;

public sealed record SayHelloResult(IReadOnlyList<Finding> Findings) : IStepResult;
