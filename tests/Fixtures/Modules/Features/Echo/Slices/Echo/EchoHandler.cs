namespace Echo.Slices.Echo;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class EchoHandler : CommandHandler<EchoCommand, string> {
    public override Task<Result<string>> HandleAsync(Envelope<EchoCommand> envelope) =>
        Task.FromResult<Result<string>>(envelope.Message.Text);
}
