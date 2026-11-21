namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public interface IRunner {
    Task RunAsync(Envelope envelope, Func<Envelope, Task> run);
}
