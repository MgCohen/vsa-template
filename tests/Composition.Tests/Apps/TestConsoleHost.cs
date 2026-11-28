namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Hosts.Console;
using Umpire.Kernels;

public sealed class TestConsoleHost : ConsoleHost, ITestHost {
    public List<Action<IServiceCollection>> Actions { get; } = [];

    public string[] Line {
        get => Args;
        set => Args = value;
    }

    protected override AppTypes TypesOf(App app) => new(ModuleProvider);

    protected override void Configure(IServiceCollection services, AppTypes types) {
        foreach (var add in Actions) {
            add(services);
        }
    }
}
