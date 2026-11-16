namespace Umpire.Vsa.Contracts.Commands;

using Umpire.Messages.Contracts;

public interface ICommandSender {
    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct);
}
