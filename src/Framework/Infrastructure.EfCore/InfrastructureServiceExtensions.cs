using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VSA.Domain;

namespace VSA.Infrastructure.EfCore;

public static class InfrastructureServiceExtensions {
    // Registers the context of a module and one default EfRepository for each of its aggregates.
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder> options) where TContext : ModuleDbContext, IModuleDbContext {
        services.AddDbContext<TContext>(options);
        foreach (var aggregate in TContext.AggregateTypes) {
            var id = IdOf(aggregate);
            var implementation = typeof(EfRepository<,>).MakeGenericType(aggregate, id);
            services.AddScoped(typeof(IRepository<,>).MakeGenericType(aggregate, id),
                provider => ActivatorUtilities.CreateInstance(provider, implementation, provider.GetRequiredService<TContext>()));
        }
        services.AddHostedService<ModuleDatabaseInitializer<TContext>>();
        return services;
    }

    static Type IdOf(Type aggregate) {
        for (var type = aggregate.BaseType; type is not null; type = type.BaseType) {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>)) {
                return type.GetGenericArguments()[0];
            }
        }
        throw new InvalidOperationException($"{aggregate.Name} does not inherit AggregateRoot<TId>.");
    }
}
