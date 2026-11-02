using Notes.Contracts.Events;
using Notes.Contracts.Requests.DeleteNote;
using Notes.Domain;
using VSA.Testing.Unit;

namespace Notes.Tests;

// A handler that reads: With puts a note in the repository before the test acts.
public sealed class DeleteNoteTests : VsaUnitTest {
    [Test]
    public async Task Deletes_the_note_and_publishes_NoteDeleted() {
        var note = Note.Create(new AuthorId(Guid.NewGuid()), "Old note").Value;
        Repository<Note, NoteId>().With(note);

        var result = await Handle(new DeleteNoteRequest(note.Id.Value));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(Repository<Note, NoteId>().Saved, Is.Empty);
        Assert.That(Events.OfType<NoteDeleted>().Single().NoteId, Is.EqualTo(note.Id.Value));
    }

    [Test]
    public async Task Unknown_note_fails() {
        var result = await Handle(new DeleteNoteRequest(Guid.NewGuid()));

        Assert.That(result.Error, Is.EqualTo(NoteErrors.NotFound));
    }
}
