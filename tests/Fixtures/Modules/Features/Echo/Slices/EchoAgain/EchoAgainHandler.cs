using Echo.Slices.Echo;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace Echo.Slices.EchoAgain;

sealed class EchoAgainHandler : CommandHandler<EchoCommand, string> {
    public override Task<Result<string>> HandleAsync(Envelope<EchoCommand> envelope) =>
        Task.FromResult<Result<string>>(envelope.Message.Text + envelope.Message.Text);
}
