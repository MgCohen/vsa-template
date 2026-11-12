using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Echo.Slices.Echo;

sealed class EchoHandler : CommandHandler<EchoCommand, string> {
    public override Task<Result<string>> HandleAsync(Envelope<EchoCommand> envelope) =>
        Task.FromResult<Result<string>>(envelope.Message.Text);
}
