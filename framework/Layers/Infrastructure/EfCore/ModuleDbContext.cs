using Microsoft.EntityFrameworkCore;

namespace VSA.Infrastructure.EfCore;

// Maps all aggregates of a module by convention. A source generator writes one subclass
// for each module, for example NotesDbContext, so EF Core and its migrations get one type per module.
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options) {
    protected abstract IReadOnlyList<Type> Aggregates { get; }

    protected override void OnModelCreating(ModelBuilder model) {
        foreach (var aggregate in Aggregates) {
            var entity = model.Entity(aggregate);
            foreach (var property in aggregate.GetProperties()) {
                if (StronglyTypedId.Converter(property.PropertyType) is { } converter) {
                    entity.Property(property.Name).HasConversion(converter);
                }
            }
            entity.Property("Id").ValueGeneratedNever();
        }
        // Optional IEntityTypeConfiguration<T> classes of the module (EF Core standard).
        model.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
