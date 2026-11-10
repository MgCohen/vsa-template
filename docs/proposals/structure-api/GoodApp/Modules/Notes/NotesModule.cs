using Microsoft.Extensions.DependencyInjection;
using GoodApp.Modules.Notes.Domain;
using GoodApp.Modules.Notes.Domain.Shared;
using GoodApp.Modules.Notes.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;

namespace GoodApp.Modules.Notes;

public sealed class NotesModule : Module
{
    public override void Configure(ModuleBuilder module)
    {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
