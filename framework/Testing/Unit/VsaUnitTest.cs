using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace VSA.Testing.Unit;

// The NUnit base of a unit test: it runs a request handler or a command handler without HTTP and without a database.
// Before each test, the base makes new services with the default fakes and calls Configure.
// Configure is the one place where a test class makes its fakes and mocks. Keep them in fields for the tests of the class.
//   public sealed class PublishNoteTests : VsaUnitTest {
//       IClock clock = null!;
//
//       protected override void Configure(IServiceCollection services) {
//           clock = Mock<IClock>();
//           clock.UtcNow.Returns(Today);
//       }
//
//       [Test]
//       public async Task Publishes_the_note() {
//           var result = await Handle(new PublishNoteRequest(id));
//           ...
//       }
//   }
public abstract class VsaUnitTest {
    ServiceCollection? services;
    ServiceProvider? provider;

    // Add or replace services before the first Handle call. After it, the services are read-only.
    protected IServiceCollection Services => services ?? throw new InvalidOperationException("The services exist only in a test.");

    // A change to the services after this point has no effect. MakeReadOnly makes such a change fail.
    protected IServiceProvider Provider => provider ??= Build();

    protected RecordingEventPublisher Events => Provider.GetRequiredService<RecordingEventPublisher>();

    // Default services: an InMemoryRepository for each IRepository<T, TId>, a RecordingEventPublisher, and logging.
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

    // Add or replace services for each test of the class. The default fakes are already in services.
    protected virtual void Configure(IServiceCollection services) { }

    // An NSubstitute mock of T, which the handlers get from the services. Call it in Configure, or in a test before Handle.
    protected T Mock<T>() where T : class {
        var mock = Substitute.For<T>();
        Services.AddSingleton(mock);
        return mock;
    }

    // The default repository of T. If a test replaces IRepository<T, TId>, use Provider instead.
    protected InMemoryRepository<T, TId> Repository<T, TId>() where T : AggregateRoot<TId> where TId : notnull =>
        Provider.GetRequiredService<IRepository<T, TId>>() as InMemoryRepository<T, TId>
        ?? throw new InvalidOperationException($"IRepository<{typeof(T).Name}, {typeof(TId).Name}> is replaced. Get it from Provider.");

    // Finds the handler of the request and runs it through a pipeline with that one handler.
    // The handler is in the Handlers.Requests project of the same module: Notes.Contracts.Requests -> Notes.Handlers.Requests.
    protected Task<Result<TResponse>> Handle<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse {
        ArgumentNullException.ThrowIfNull(request);
        var pipeline = new PipelineBuilder<IMessage>().Handlers(HandlerOf(request.GetType())).Build(Provider);
        return pipeline.SendAsync<TResponse>(request, ct);
    }

    // Finds the handler of the command, creates it with the services, and runs it.
    // The handler is in the Handlers.Commands project of the same module: Cards.Contracts.Commands -> Cards.Handlers.Commands.
    // A handler that sends other commands gets ICommandSender from the services: add a mock of it in Configure.
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
        var found = handlers.GetTypes().Where(t => !t.IsAbstract && HandledRequest(t) == request).ToList();
        return found.Count == 1 ? found[0]
            : throw new InvalidOperationException($"{request.FullName} has {found.Count} handlers in {handlers.GetName().Name}. It must have one.");
    }

    static Type CommandHandlerOf(Type command, Type service) {
        var name = command.Assembly.GetName().Name!;
        var handlers = Assembly.Load(name.Replace(".Contracts.Commands", ".Handlers.Commands", StringComparison.Ordinal));
        var found = handlers.GetTypes().Where(t => !t.IsAbstract && service.IsAssignableFrom(t)).ToList();
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
