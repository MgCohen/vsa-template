namespace Notes.Tests;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notes.Features.Notebook.Domain;
using Notes.Features.Notebook.Slices.CreateNote;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Testing.Unit;

public sealed class CreateNotePublisherTests : VsaUnitTest {
    IIntegrationEventPublisher publisher = null!;

    [Test]
    public void Failure_of_the_publisher_stops_the_request() =>
        Assert.ThrowsAsync<TimeoutException>(() => Handle(new CreateNoteRequest(Guid.NewGuid(), "Title")));

    protected override void Configure(IServiceCollection services) {
        publisher = Mock<IIntegrationEventPublisher>();
        publisher.PublishAsync(Arg.Any<NoteCreated>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());
    }
}
