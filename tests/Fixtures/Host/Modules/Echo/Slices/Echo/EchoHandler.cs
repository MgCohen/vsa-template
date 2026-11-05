using VSA.Handlers.Commands;

namespace Echo.Slices.Echo;

sealed class EchoHandler : CommandHandler<EchoCommand, string> {
    public override Task<string> HandleAsync(EchoCommand command, CancellationToken ct) => Task.FromResult(command.Text);
}
