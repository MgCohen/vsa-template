using Report.Domain.Shared;

namespace Hello.Slices.SayHello;

public sealed record SayHelloResult(IReadOnlyList<Finding> Findings);
