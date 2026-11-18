namespace Composition.Tests;

using Umpire.Kernels;

public sealed class TestBuild(AppHost host, IServiceProvider services) : IDisposable {
    public IServiceProvider Services { get; } = services;

    public void Dispose() => host.Dispose();
}
