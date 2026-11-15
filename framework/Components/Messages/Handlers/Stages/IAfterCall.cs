using Umpire.Markers;
using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

[RequireImplementors]
public interface IAfterCall {
    Task CallAsync(Envelope envelope);
}
