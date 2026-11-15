namespace Notes.Features.Notes;

using Microsoft.Extensions.DependencyInjection;
using global::Notes.Features.Notes.Domain;
using global::Notes.Features.Notes.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;
using VSA.Kernel;

public sealed class NotesModule : Module {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
