namespace Notes.Features.Notebook;

using Microsoft.Extensions.DependencyInjection;
using Notes.Features.Notebook.Domain;
using Notes.Features.Notebook.Infrastructure;
using Umpire.Vsa.Infrastructure;
using Umpire.Kernels;

public sealed class NotebookModule : Module {
    public override void Configure(IServiceCollection services) {
        services.AddSingleton<NoteStore>();
        services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
