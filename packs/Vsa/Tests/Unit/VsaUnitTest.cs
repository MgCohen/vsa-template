namespace Umpire.Vsa.Testing.Unit;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Utility;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Domain;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Infrastructure;

public abstract class VsaUnitTest {
    ServiceCollection? services;
    UnitTestApp<VsaKernel>? app;
    UnitTestHost? host;
    AppInstance? running;

    protected IServiceCollection Services => services ?? throw new InvalidOperationException("The services exist only in a test.");

    protected IServiceProvider Provider => Started().Provider;

    protected RecordingEventPublisher Events => Provider.GetRequiredService<RecordingEventPublisher>();

    protected virtual IModuleProvider Modules => new ScannedModules();

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
        var stopping = running;
        app = null;
        host = null;
        running = null;
        stopping?.Stop();
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
        Started();
        return host!.SendAsync(request, ct);
    }

    protected async Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(command);
        using var scope = Provider.CreateScope();
        var pipeline = new PipelineBuilder<IMessage>()
            .Handlers(HandlerOf(typeof(CommandHandler<,>), command.GetType()))
            .CallsCommandHandlers()
            .Build(scope.ServiceProvider);
        return await pipeline.SendAsync<TResult>(command, ct).ConfigureAwait(false);
    }

    UnitTestApp<VsaKernel> Started() {
        if (app is not null) {
            return app;
        }
        var all = services ?? throw new InvalidOperationException("The services exist only in a test.");
        all.MakeReadOnly();
        var started = new UnitTestApp<VsaKernel>();
        var testHost = new UnitTestHost { TestServices = all, ModuleProvider = Modules };
        running = started.Run(testHost);
        host = testHost;
        return app = started;
    }

    static Type HandlerOf(Type handlerBase, Type message) {
        var found = LoadedTypes.Of(message.Assembly).Where(t => !t.IsAbstract && HandledMessage(t, handlerBase) == message).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{message.FullName} has {found.Count} handlers in {message.Assembly.GetName().Name}. It must have one.");
    }

    static Type? HandledMessage(Type type, Type handlerBase) => ClosedBase.Of(type, handlerBase)?.GetGenericArguments()[0];
}
