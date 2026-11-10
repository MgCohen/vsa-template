namespace BadApp.Modules.Notes;

public class NotesModule : Fw.ModuleBase
{
    public Type[] Handlers => [typeof(Slices.CreateNote.CreateNoteHandler), typeof(Slices.ArchiveNote.ArchiveNoteHandler)];
}
