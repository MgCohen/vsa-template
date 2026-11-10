using Microsoft.Extensions.DependencyInjection;
using BadApp.Modules.Notes.Domain;
using BadApp.Modules.Notes.Domain.Shared;
using BadApp.Modules.Notes.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;

namespace BadApp.Modules.Notes;

public sealed class NotesModule : Module
{
    public override void Configure(ModuleBuilder module)
    {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
