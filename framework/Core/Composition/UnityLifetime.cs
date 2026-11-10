using Microsoft.Extensions.Hosting;

namespace VSA.Composition;

sealed class UnityLifetime : IHostLifetime {
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
