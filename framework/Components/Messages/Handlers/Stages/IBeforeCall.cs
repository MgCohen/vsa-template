namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IBeforeCall {
    Task<object> CallAsync(Envelope envelope);
}
