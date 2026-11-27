namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class TestHost : AppHost, ITestHost {
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
