# Persistence

VSA has one persistence port, `IRepository<T, TId>` (`framework/Architectures/Vsa/Infrastructure/IRepository.cs`). VSA has no storage adapter. Each module writes its adapter in `Infrastructure/` and registers it in its module root.

## The port

| Member | Behavior |
|---|---|
| `GetAsync(id, ct)` | Gives the aggregate with this id, or `null` |
| `ListAsync(where, ct)` | Gives the aggregates that match the filter |
| `Add(aggregate)`, `Remove(aggregate)` | Records a change. The change has no effect before `SaveChangesAsync` |
| `SaveChangesAsync(ct)` | Writes all recorded changes |

`ListAsync` takes a `Func<T, bool>`, so an adapter filters the aggregates in memory. A query language cannot read a `Func`.

## Register an adapter

```csharp
public sealed class NotesModule : Module {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddSingleton<NoteStore>();
        module.Services.AddScoped<IRepository<Note, NoteId>, NoteRepository>();
    }
}
```

The sample `samples/Notes` keeps its notes in memory (`Infrastructure/NoteRepository.cs`). Unit tests use `InMemoryRepository<T, TId>` from `VSA.Testing.Unit`.

## Non-goals

- **No EF Core package.** EF Core does not run in Unity with IL2CPP. The game consumers keep their state in an ECS world, and write it as an event log and snapshots. A .NET server consumer can add EF Core in the `Infrastructure/` project of its module.

## Open questions

- Should VSA ship a general in-memory adapter or a snapshot adapter for `IRepository<T, TId>`?
