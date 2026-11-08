namespace VSA.Contracts.Commands;

// Here, so each handler can send a command without a reference to Handlers.Commands.
// An interface: in process now, a fake in tests.
public interface ICommandSender {
    // Runs the one handler of the command and gives its result. A command with no handler is an error.
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct);
}
