using Echo.Slices.Echo;
using VSA.Handlers.Commands;

namespace Echo.Slices.EchoAgain;

sealed class EchoAgainHandler : CommandHandler<EchoCommand, string> {
    public override Task<string> HandleAsync(EchoCommand command, CancellationToken ct) => Task.FromResult(command.Text + command.Text);
}
