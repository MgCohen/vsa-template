using Report.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Hello.Slices.SayHello;

sealed class SayHelloHandler : CommandHandler<SayHelloCommand, SayHelloResult> {
    public override Task<Result<SayHelloResult>> HandleAsync(Envelope<SayHelloCommand> envelope) {
        var hello = new Finding("HELLO", FindingSeverity.Info, "Hello from a plugin");
        return Task.FromResult<Result<SayHelloResult>>(new SayHelloResult([hello]));
    }
}
