namespace Greet.Slices.Greet;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class GreetHandler : CommandHandler<GreetCommand, GreetResult> {
    public override Task<Result<GreetResult>> HandleAsync(Envelope<GreetCommand> envelope) {
        var command = envelope.Message;
        if (command.Name.Length == 0) {
            return Task.FromResult<Result<GreetResult>>(new Error("greet.name", "The name is empty."));
        }
        return Task.FromResult<Result<GreetResult>>(new GreetResult($"Hello, {command.Name}", command.Style == GreetStyle.Loud ? 10 : command.Code));
    }
}
