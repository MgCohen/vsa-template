namespace Umpire.Messages.Handlers;

using Umpire.Markers;
using Umpire.Messages.Contracts;

[RequireImplementors]
public interface IAfterCall {
    Task CallAsync(Envelope envelope);
}
