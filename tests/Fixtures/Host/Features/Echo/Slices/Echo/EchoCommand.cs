namespace Echo.Slices.Echo;

using Umpire.Vsa.Contracts.Commands;

public sealed record EchoCommand(string Text) : ICommand<string>;
