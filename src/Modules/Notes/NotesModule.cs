using Microsoft.EntityFrameworkCore;
using Notes.Infrastructure;
using VSA.Infrastructure.EfCore;
using VSA.Module;

namespace Notes;

public sealed class NotesModule : Module {
    public override string Name => "Notes";

    public override void Configure(ModuleBuilder module) {
        var settings = module.Settings<NotesSettings>();
        module.Services.AddModuleDbContext<NotesDbContext>(o => o.UseSqlite(settings.ConnectionString));
    }
}
