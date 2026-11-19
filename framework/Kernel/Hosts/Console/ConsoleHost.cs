namespace Umpire.Hosts.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    ServiceProvider? provider;
    IConfiguration? configuration;
    Operation? operation;
    AppSignals? signals;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        configuration = new ConfigurationBuilder().AddCommandLine(CommandLine.Options(args)).Build();
        var services = new ServiceCollection();
        Compose(services, configuration, app);
        provider = services.BuildServiceProvider();
        operation = CommandLine.Select(provider.GetServices<Operation>().ToList(), args);
        return provider;
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        signals = AppSignals.Listen(stop);
        work = Task.Run(() => CommandLine.Run(services, operation!, configuration!, stop, stopping.Token));
    }

    public override void Dispose() {
        signals?.Dispose();
        signals = null;
        stopping.Cancel();
        try {
            work?.GetAwaiter().GetResult();
        } catch (OperationCanceledException) {
        }
        provider?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        provider = null;
        stopping.Dispose();
        base.Dispose();
    }
}
