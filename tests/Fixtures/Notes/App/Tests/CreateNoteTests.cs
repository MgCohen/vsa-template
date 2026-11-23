namespace Notes.Tests;

using Notes.Features.Notebook.Domain;
using Notes.Features.Notebook.Slices.CreateNote;
using Umpire.Vsa.Testing.Unit;

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
