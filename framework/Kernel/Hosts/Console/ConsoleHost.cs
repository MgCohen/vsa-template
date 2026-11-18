namespace Umpire.Hosts.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    ServiceProvider? provider;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        var configuration = new ConfigurationBuilder().AddCommandLine(CommandLine.Options(args)).Build();
        var services = new ServiceCollection();
        Compose(services, configuration, app);
        provider = services.BuildServiceProvider();
        var operation = CommandLine.Select(provider.GetServices<Operation>().ToList(), args);
        work = CommandLine.Run(provider, operation, configuration, stopping.Token);
        return provider;
    }

    public override void Dispose() {
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
