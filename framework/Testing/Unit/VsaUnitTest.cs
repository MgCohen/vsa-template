using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Testing.Unit;

// The NUnit base of a unit test. Each test gets a new HandlerHost.
//   public sealed class CreateNoteTests : VsaUnitTest {
//       [Test]
//       public async Task Creates_a_note() {
//           var result = await Handle(new CreateNoteRequest(author, "Title"));
//           ...
//       }
//   }
public abstract class VsaUnitTest {
    HandlerHost? host;

    protected HandlerHost Host => host ?? throw new InvalidOperationException("The host exists only in a test.");
    protected IServiceCollection Services => Host.Services;
    protected RecordingEventPublisher Events => Host.Events;

    [SetUp]
    public void CreateHost() => host = new HandlerHost();

    protected InMemoryRepository<T, TId> Repository<T, TId>() where T : AggregateRoot<TId> where TId : notnull => Host.Repository<T, TId>();

    protected Task<Result<TResponse>> Handle<TResponse>(IRequest<TResponse> request) => Host.Handle(request);
}
