using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VSA.Domain;
using VSA.Module;

namespace VSA.Infrastructure.EfCore;

public static class InfrastructureServiceExtensions {
    // Registers the database of the module. The generator writes the context into <Module>.Infrastructure
    // as an internal type, so this method finds it there. The module gives only the provider.
    public static ModuleBuilder AddDatabase(this ModuleBuilder module, Action<DbContextOptionsBuilder> options) {
        var infrastructure = Assembly.Load($"{module.Name}.Infrastructure");
        var context = infrastructure.GetTypes().SingleOrDefault(t => !t.IsAbstract && typeof(IModuleDbContext).IsAssignableFrom(t))
            ?? throw new InvalidOperationException($"{infrastructure.GetName().Name} has no module context. Make sure that VSA.Analyzers runs on it.");
        typeof(InfrastructureServiceExtensions).GetMethod(nameof(AddModuleDbContext), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(context).Invoke(null, [module.Services, options]);
        return module;
    }

    // Registers the context of a module and one default EfRepository for each of its aggregates.
    static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services,
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
