namespace Greet.Slices.Greet;

using Umpire.Vsa.Contracts.Requests;

public sealed record GreetResponse(string Text, int ExitCode) : IResponse, IExitCode;
