using VSA.Contracts.Commands;

namespace Echo.Slices.Echo;

public sealed record EchoCommand(string Text) : ICommand<string>;
