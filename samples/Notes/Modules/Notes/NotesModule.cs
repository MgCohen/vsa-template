using Notes.Domain;
using Notes.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;

namespace Notes;

public sealed class NotesModule : Module {
    public override void Configure(ModuleBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
