using Umpire.Vsa.Contracts.Commands;

namespace Echo.Slices.Echo;

public sealed record EchoCommand(string Text) : ICommand<string>;
