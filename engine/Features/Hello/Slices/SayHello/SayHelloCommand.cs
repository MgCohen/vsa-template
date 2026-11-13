using VSA.Contracts.Commands;

namespace Hello.Slices.SayHello;

public sealed record SayHelloCommand(string AppRoot) : ICommand<SayHelloResult>;
