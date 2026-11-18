namespace Engine.Tests;

using Umpire.Kernels;

sealed class EngineBuild(AppHost host, IServiceProvider services) : IDisposable {
    public IServiceProvider Services { get; } = services;

    public void Dispose() => host.Dispose();
}
