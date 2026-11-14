using Microsoft.Extensions.DependencyInjection;
using Notes.Domain;
using Notes.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;
using VSA.Kernel;

namespace Notes;

public sealed class NotesModule : Module {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
