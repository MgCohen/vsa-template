using Echo.Slices.Echo;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Echo.Slices.EchoAgain;

sealed class EchoAgainHandler : CommandHandler<EchoCommand, string> {
    public override Task<Result<string>> HandleAsync(Envelope<EchoCommand> envelope) =>
        Task.FromResult<Result<string>>(envelope.Message.Text + envelope.Message.Text);
}
