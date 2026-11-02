using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace VSA.Testing.Unit;

// Runs a request handler without HTTP and without a database. It has no test framework type.
// Default services: an InMemoryRepository for each IRepository<T, TId>, a RecordingEventPublisher, and logging.
// Add or replace services before the first Handle call. After it, the services are read-only.
public sealed class HandlerHost {
    readonly ServiceCollection services = new();
    ServiceProvider? provider;

    public HandlerHost() {
        Services.AddLogging();
        Services.AddSingleton(typeof(IRepository<,>), typeof(InMemoryRepository<,>));
        Services.AddSingleton<RecordingEventPublisher>();
        Services.AddSingleton<IIntegrationEventPublisher>(s => s.GetRequiredService<RecordingEventPublisher>());
    }

    public IServiceCollection Services => services;

    // A change to the services after this point has no effect. MakeReadOnly makes such a change fail.
    public IServiceProvider Provider => provider ??= Build();

    ServiceProvider Build() {
        services.MakeReadOnly();
        return services.BuildServiceProvider();
    }

    public RecordingEventPublisher Events => Provider.GetRequiredService<RecordingEventPublisher>();

    // The default repository of T. If a test replaces IRepository<T, TId>, use Provider instead.
    public InMemoryRepository<T, TId> Repository<T, TId>() where T : AggregateRoot<TId> where TId : notnull =>
        Provider.GetRequiredService<IRepository<T, TId>>() as InMemoryRepository<T, TId>
        ?? throw new InvalidOperationException($"IRepository<{typeof(T).Name}, {typeof(TId).Name}> is replaced. Get it from Provider.");

    // Finds the handler of the request, creates it with the services, and runs it.
    // The handler is in the Handlers.Requests project of the same module: Notes.Contracts.Requests -> Notes.Handlers.Requests.
    public async Task<Result<TResponse>> Handle<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        var handler = HandlerOf(request.GetType());
        var instance = ActivatorUtilities.CreateInstance(Provider, handler);
        var method = handler.GetMethod("HandleRequestAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<Result<TResponse>>)method.Invoke(instance, [request, ct])!;
    }

    static Type HandlerOf(Type request) {
        var name = request.Assembly.GetName().Name!;
        var handlers = Assembly.Load(name.Replace(".Contracts.Requests", ".Handlers.Requests", StringComparison.Ordinal));
        var found = handlers.GetTypes().Where(t => !t.IsAbstract && HandledRequest(t) == request).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{request.FullName} has {found.Count} handlers in {handlers.GetName().Name}. It must have one.");
    }

    static Type? HandledRequest(Type type) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(RoutedHandler<,>)) {
                return b.GetGenericArguments()[0];
            }
        }
        return null;
    }
}
