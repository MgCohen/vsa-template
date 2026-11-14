using VSA.Contracts.Commands;

namespace Greet.Slices.Greet;

public sealed record GreetCommand(string Name, int Code = 0) : IConsoleCommand<GreetResult>;
