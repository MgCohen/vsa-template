using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VSA.Infrastructure.EfCore;

// Creates the database of a module at start-up if it does not exist. Migrations replace this later.
sealed class ModuleDatabaseInitializer<TContext>(IServiceScopeFactory scopes) : IHostedService
    where TContext : ModuleDbContext {
    public async Task StartAsync(CancellationToken ct) {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
