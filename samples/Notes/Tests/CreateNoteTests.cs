using Notes.Contracts.Events;
using Notes.Contracts.Requests.CreateNote;
using Notes.Domain;
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

        Assert.That(result.Error, Is.EqualTo(NoteErrors.TitleRequired));
        Assert.That(Repository<Note, NoteId>().SaveCount, Is.Zero);
    }
}
