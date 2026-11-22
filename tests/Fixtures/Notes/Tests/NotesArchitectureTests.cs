namespace Notes.Tests;

using Notes.Features.Notebook;
using Notes.Features.Notebook.Infrastructure;
using Notes.Features.Notebook.Presenters;
using Notes.Features.Notebook.Slices.CreateNote;
using Umpire.Architecture;
using Umpire.Vsa.Testing.Unit;

public sealed class NotesArchitectureTests : VsaArchitectureTest {
    [Test]
    public void The_module_class_binds_to_the_module_root() {
        Assert.That(At<NotebookModule>(Feature("Notebook")).Part, Is.EqualTo("RootClass"));
    }

    [Test]
    public void A_request_binds_to_the_request_contracts_of_its_slice() {
        var place = At<CreateNoteRequest>(Slice("Notebook", "CreateNote"));

        Assert.That(place.Part, Is.EqualTo("Contracts.Requests"));
        Assert.That(place.Values["Slice"], Is.EqualTo("CreateNote"));
    }

    [Test]
    public void The_presenter_uses_an_event_of_its_module() {
        var presenter = At<NotebookPresenter>(Feature("Notebook") + "/Presenters");
        var noteCreated = At<NoteCreated>(Slice("Notebook", "CreateNote"));

        Assert.That(MayUse(presenter, noteCreated).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void An_event_does_not_use_the_infrastructure() {
        var noteCreated = At<NoteCreated>(Slice("Notebook", "CreateNote"));
        var repository = At<NoteRepository>(Feature("Notebook") + "/Infrastructure");

        Assert.That(MayUse(noteCreated, repository).Permission, Is.EqualTo(Permission.Deny));
    }
}
