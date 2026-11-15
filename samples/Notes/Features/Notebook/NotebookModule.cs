namespace Notes.Features.Notebook;

using Microsoft.Extensions.DependencyInjection;
using Notes.Features.Notebook.Domain;
using Notes.Features.Notebook.Infrastructure;
using VSA.Infrastructure;
using VSA.Module;
using VSA.Kernel;

public sealed class NotebookModule : Module {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
