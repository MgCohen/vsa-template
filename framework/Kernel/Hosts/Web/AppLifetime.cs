namespace Umpire.Hosts.Web;

using Microsoft.Extensions.Hosting;

sealed class AppLifetime : IHostLifetime {
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
