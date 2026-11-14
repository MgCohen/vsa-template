using Steps.Contracts.Commands;

namespace Hello.Slices.SayHello;

public sealed record SayHelloCommand(string AppRoot) : EngineStep<SayHelloResult>;
