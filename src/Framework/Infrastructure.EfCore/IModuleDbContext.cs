namespace VSA.Infrastructure.EfCore;

// The source generator implements this on the context type of each module.
public interface IModuleDbContext {
    static abstract IReadOnlyList<Type> AggregateTypes { get; }
}
