namespace Umpire.Messages.Handlers;

using Umpire.Markers;
using Umpire.Messages.Contracts;

[RequireImplementors]
public interface IBeforeCall {
    Task<object> CallAsync(Envelope envelope);
}
