using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notes.Domain;
using Notes.Slices.CreateNote;
using VSA.Contracts.Events;
using VSA.Testing.Unit;

namespace Notes.Tests;

// A unit test of one slice: the request goes to its handler, with the default fakes.
public sealed class CreateNoteTests : VsaUnitTest {
    [Test]
    public async Task Creates_the_note_and_publishes_NoteCreated() {
        var author = Guid.NewGuid();

        var result = await Handle(new CreateNoteRequest(author, " First note "));

        Assert.That(result.IsSuccess, Is.True);
        var note = Repository<Note, NoteId>().Saved.Single();
        Assert.That(note.Title, Is.EqualTo("First note"));
        Assert.That(Events.OfType<NoteCreated>().Single().NoteId, Is.EqualTo(result.Value.Id));
    }

    [Test]
    public async Task Empty_title_fails_and_saves_nothing() {
        var result = await Handle(new CreateNoteRequest(Guid.NewGuid(), " "));

        Assert.That(result.Error?.Code, Is.EqualTo("notes.title_required"));
        Assert.That(Repository<Note, NoteId>().SaveCount, Is.Zero);
    }
}

// Configure makes the mocks one time for each test. The tests of the class use them through the field.
public sealed class CreateNotePublisherTests : VsaUnitTest {
    IIntegrationEventPublisher publisher = null!;

    protected override void Configure(IServiceCollection services) {
        publisher = Mock<IIntegrationEventPublisher>();
        publisher.PublishAsync(Arg.Any<NoteCreated>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());
    }

    [Test]
    public void Failure_of_the_publisher_stops_the_request() =>
        Assert.ThrowsAsync<TimeoutException>(() => Handle(new CreateNoteRequest(Guid.NewGuid(), "Title")));
}
