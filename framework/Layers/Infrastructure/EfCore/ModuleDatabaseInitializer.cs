using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VSA.Infrastructure.EfCore;

// Brings the database of a module up to date at start-up. A module with migrations gets them applied
// in order. A module with no migrations gets its tables created from the model.
sealed class ModuleDatabaseInitializer<TContext>(IServiceScopeFactory scopes) : IHostedService
    where TContext : ModuleDbContext {
    public async Task StartAsync(CancellationToken ct) {
        await using var scope = scopes.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<TContext>().Database;
        if (database.GetMigrations().Any()) {
            await database.MigrateAsync(ct);
        } else {
            await database.EnsureCreatedAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
