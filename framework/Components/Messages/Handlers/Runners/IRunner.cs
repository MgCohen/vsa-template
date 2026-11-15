using Umpire.Markers;
using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

[RequireImplementors]
public interface IRunner {
    Task RunAsync(Envelope envelope, Func<Envelope, Task> run);

    Task StartAsync();
}
