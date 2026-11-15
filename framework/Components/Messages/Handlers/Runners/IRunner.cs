namespace Umpire.Messages.Handlers;

using Umpire.Markers;
using Umpire.Messages.Contracts;

[RequireImplementors]
public interface IRunner {
    Task RunAsync(Envelope envelope, Func<Envelope, Task> run);

    Task StartAsync();
}
