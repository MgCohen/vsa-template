namespace Greet.Slices.Greet;

using Umpire.Vsa.Contracts.Commands;

public sealed record GreetResult(string Text, int ExitCode) : IExitCode;
