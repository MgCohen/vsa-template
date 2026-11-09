using VSA.Contracts;

namespace VSA.Handlers;

public interface IRunner {
    Task RunAsync(Envelope envelope, Func<Envelope, Task> run);

    Task StartAsync();
}
