using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Greet.Slices.Greet;

sealed class GreetHandler : CommandHandler<GreetCommand, GreetResult> {
    public override Task<Result<GreetResult>> HandleAsync(Envelope<GreetCommand> envelope) {
        var command = envelope.Message;
        if (command.Name.Length == 0) {
            return Task.FromResult<Result<GreetResult>>(new Error("greet.name", "The name is empty."));
        }
        return Task.FromResult<Result<GreetResult>>(new GreetResult($"Hello, {command.Name}", command.Code));
    }
}
