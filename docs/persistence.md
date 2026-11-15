# Persistence

VSA has one persistence port, `IRepository<T, TId>` (`framework/Architectures/Vsa/Infrastructure/IRepository.cs`). VSA has no storage adapter. Each module writes its adapter in `Infrastructure/` and registers it in its module root.

## The domain base

`Entity<TId>` (`framework/Architectures/Vsa/Domain/Entity.cs`) is the one domain base of VSA. It holds only `Id`. It has no behavior.

| Type | Constraint |
|---|---|
| `IRepository<T, TId>` | `where T : Entity<TId>` |
| `InMemoryRepository<T, TId>` | `where T : Entity<TId>` |
| `VsaUnitTest.Repository<T, TId>()` | `where T : Entity<TId>` |

The repository needs `Id`, because `GetAsync(id)` and the in-memory store find an entity by its id. So the type of the repository inherits `Entity<TId>`.

VSA has no `AggregateRoot<TId>`. It was a base for EF Core, and EF Core is not in VSA. An aggregate base had no behavior, and only the repository constraint used it (issue #121).

## The port

| Member | Behavior |
|---|---|
| `GetAsync(id, ct)` | Gives the entity with this id, or `null` |
| `ListAsync(where, ct)` | Gives the entities that match the filter |
| `Add(entity)`, `Remove(entity)` | Records a change. The change has no effect before `SaveChangesAsync` |
| `SaveChangesAsync(ct)` | Writes all recorded changes |

`ListAsync` takes a `Func<T, bool>`, so an adapter filters the entities in memory. A query language cannot read a `Func`.

## Register an adapter

```csharp
public sealed class NotebookModule : Module {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
```

The sample `samples/Notes` keeps its notes in memory (`Infrastructure/NoteRepository.cs`). Unit tests use `InMemoryRepository<T, TId>` from `VSA.Testing.Unit`. `VsaUnitTest` registers it for each `IRepository<T, TId>`. `Add` and `Remove` have no effect before `SaveChangesAsync`, as in a real adapter.

## Non-goals

- **No EF Core package.** EF Core does not run in Unity with IL2CPP. The game consumers keep their state in an ECS world, and write it as an event log and snapshots. A .NET server consumer can add EF Core in the `Infrastructure/` project of its module.

## Open questions

- Should VSA ship a general in-memory adapter or a snapshot adapter for `IRepository<T, TId>`?
