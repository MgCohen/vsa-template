namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IAfterCall {
    Task CallAsync(Envelope envelope);
}
