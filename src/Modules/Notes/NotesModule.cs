using Microsoft.EntityFrameworkCore;
using Notes.Infrastructure;
using VSA.Infrastructure.EfCore;
using VSA.Module;

namespace Notes;

public sealed class NotesModule : Module<NotesSettings> {
    protected override IEnumerable<string> Validate(NotesSettings settings) {
        if (!settings.ConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)) {
            yield return "ConnectionString must be a SQLite connection string with Data Source.";
        }
    }

    public override void Configure(ModuleBuilder module, NotesSettings settings) =>
        module.Services.AddModuleDbContext<NotesDbContext>(o => o.UseSqlite(settings.ConnectionString));
}
