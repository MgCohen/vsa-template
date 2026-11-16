namespace VsaApp.Plugins.Tools.Infrastructure;

using VsaApp.Features.Notebook.Domain;

public sealed class NoteReader {
    public static string Read(Note note) => note.Title;
}
