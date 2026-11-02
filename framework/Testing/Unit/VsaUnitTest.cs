using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Testing.Unit;

// The NUnit base of a unit test. Before each test, the base creates a new HandlerHost and calls Configure.
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
    HandlerHost? host;

    protected HandlerHost Host => host ?? throw new InvalidOperationException("The host exists only in a test.");
    protected IServiceCollection Services => Host.Services;
    protected RecordingEventPublisher Events => Host.Events;

    [SetUp]
    public void CreateHost() {
        host = new HandlerHost();
        Configure(Host.Services);
    }

    // Add or replace services for each test of the class. The default fakes are already in services.
    protected virtual void Configure(IServiceCollection services) { }

    // An NSubstitute mock of T, which the handlers get from the services. Call it in Configure, or in a test before Handle.
    protected T Mock<T>() where T : class {
        var mock = Substitute.For<T>();
        Services.AddSingleton(mock);
        return mock;
    }

    protected InMemoryRepository<T, TId> Repository<T, TId>() where T : AggregateRoot<TId> where TId : notnull => Host.Repository<T, TId>();

    protected Task<Result<TResponse>> Handle<TResponse>(IRequest<TResponse> request) => Host.Handle(request);
}
