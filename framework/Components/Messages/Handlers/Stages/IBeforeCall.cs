using Umpire.Markers;
using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

[RequireImplementors]
public interface IBeforeCall {
    Task<object> CallAsync(Envelope envelope);
}
