namespace Umpire.Vsa.Testing.Unit;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
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
        var pipeline = new PipelineBuilder<IMessage>().Handlers(HandlerOf(typeof(RequestHandler<,>), request.GetType())).Build(Provider);
        return pipeline.SendAsync<TResponse>(request, ct);
    }

    protected Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(command);
        var pipeline = new PipelineBuilder<IMessage>().Handlers(HandlerOf(typeof(CommandHandler<,>), command.GetType())).Build(Provider);
        return pipeline.SendAsync<TResult>(command, ct);
    }

    ServiceProvider Build() {
        var all = services ?? throw new InvalidOperationException("The services exist only in a test.");
        all.MakeReadOnly();
        return all.BuildServiceProvider();
    }

    static Type HandlerOf(Type handlerBase, Type message) {
        var found = LoadedTypes.Of(message.Assembly).Where(t => !t.IsAbstract && HandledMessage(t, handlerBase) == message).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{message.FullName} has {found.Count} handlers in {message.Assembly.GetName().Name}. It must have one.");
    }

    static Type? HandledMessage(Type type, Type handlerBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == handlerBase) {
                return b.GetGenericArguments()[0];
            }
        }
        return null;
    }
}
