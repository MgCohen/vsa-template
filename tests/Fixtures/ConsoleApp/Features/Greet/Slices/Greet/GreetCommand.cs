namespace Greet.Slices.Greet;

using Umpire.Vsa.Contracts.Commands;

public sealed record GreetCommand(string Name, int Code = 0) : IConsoleCommand<GreetResult>;
