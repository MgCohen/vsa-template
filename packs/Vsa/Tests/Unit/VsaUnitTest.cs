namespace Umpire.Vsa.Testing.Unit;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Domain;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

public abstract class VsaUnitTest {
    ServiceCollection? services;
    ServiceProvider? provider;

    protected IServiceCollection Services => services ?? throw new InvalidOperationException("The services exist only in a test.");

    protected IServiceProvider Provider => provider ??= Build();

    protected RecordingEventPublisher Events => Provider.GetRequiredService<RecordingEventPublisher>();

    [SetUp]
    public void CreateServices() {
        services = [];
        services.AddLogging();
        services.AddSingleton(typeof(IRepository<,>), typeof(InMemoryRepository<,>));
        services.AddSingleton<RecordingEventPublisher>();
        services.AddSingleton<IIntegrationEventPublisher>(s => s.GetRequiredService<RecordingEventPublisher>());
        Configure(services);
    }

    [TearDown]
    public void DisposeServices() {
        provider?.Dispose();
        provider = null;
    }

    protected virtual void Configure(IServiceCollection services) { }

    protected T Mock<T>() where T : class {
        var mock = Substitute.For<T>();
        Services.AddSingleton(mock);
        return mock;
    }

    protected InMemoryRepository<T, TId> Repository<T, TId>() where T : Entity<TId> where TId : notnull =>
        Provider.GetRequiredService<IRepository<T, TId>>() as InMemoryRepository<T, TId>
        ?? throw new InvalidOperationException($"IRepository<{typeof(T).Name}, {typeof(TId).Name}> is replaced. Get it from Provider.");

    protected Task<Result<TResponse>> Handle<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse {
        ArgumentNullException.ThrowIfNull(request);
        var pipeline = new PipelineBuilder<IMessage>().Handlers(HandlerOf(request.GetType())).Build(Provider);
        return pipeline.SendAsync<TResponse>(request, ct);
    }

    protected Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(command);
        var service = typeof(CommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = CommandHandlerOf(command.GetType(), service);
        var pipeline = new PipelineBuilder<IMessage>().Handlers(handler).Build(Provider);
        return pipeline.SendAsync<TResult>(command, ct);
    }

    ServiceProvider Build() {
        var all = services ?? throw new InvalidOperationException("The services exist only in a test.");
        all.MakeReadOnly();
        return all.BuildServiceProvider();
    }

    static Type HandlerOf(Type request) {
        var name = request.Assembly.GetName().Name!;
        var handlers = Assembly.Load(name.Replace(".Contracts.Requests", ".Handlers.Requests", StringComparison.Ordinal));
        var found = LoadedTypes.Of(handlers).Where(t => !t.IsAbstract && HandledRequest(t) == request).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{request.FullName} has {found.Count} handlers in {handlers.GetName().Name}. It must have one.");
    }

    static Type CommandHandlerOf(Type command, Type service) {
        var name = command.Assembly.GetName().Name!;
        var handlers = Assembly.Load(name.Replace(".Contracts.Commands", ".Handlers.Commands", StringComparison.Ordinal));
        var found = LoadedTypes.Of(handlers).Where(t => !t.IsAbstract && service.IsAssignableFrom(t)).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{command.FullName} has {found.Count} handlers in {handlers.GetName().Name}. It must have one.");
    }

    static Type? HandledRequest(Type type) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(RequestHandler<,>)) {
                return b.GetGenericArguments()[0];
            }
        }
        return null;
    }
}
