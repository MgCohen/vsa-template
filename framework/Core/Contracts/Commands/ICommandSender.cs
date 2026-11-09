using VSA.Contracts;

namespace VSA.Contracts.Commands;

// Here, so each handler can send a command without a reference to Handlers.Commands.
// An interface: in process now, a fake in tests.
public interface ICommandSender {
    // Runs the command through its pipeline and gives the result of its handler. A command with no handler is an error.
    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct);
}
