# Proposal: rename Shared modules to Plugins

[Index](README.md)

The root folder `Shared/` becomes `Plugins/`, and a Shared module becomes a plugin module. The word "Shared" has three meanings in VSA, and a term must have one meaning. "Plugin" also tells the reader the function of the module: it extends the framework for each module.

## The problem: one word, three meanings

| Term | Meaning | Who sees it |
|---|---|---|
| `Shared/<Module>/` | A base module of the app: the base of the same layer in each module | All modules |
| `Domain/Shared/`, `<Module>.Domain.Shared` | The published domain of one module | Modules in `VsaModuleUses` |
| `Slices/Shared/` | Contracts that two or more slices of one module use | One module |

A sentence such as "a Shared module uses the `Domain.Shared` of each Shared module" is correct, but hard to read.

## The design

```
Plugins/Game/
├─ Game.csproj, GamePlugin.cs
├─ Projects/   Game.Contracts.Commands.csproj  Game.Handlers.Commands.csproj
├─ Contracts/Commands/   GameStep.cs
└─ Handlers/Commands/    ValidateGameStep.cs, GameScopeStep.cs
```

| Current name | Plugin name |
|---|---|
| Folder `Shared/` | `Plugins/` |
| `VsaRootFolders`: `Modules;Shared` | `Modules;Plugins` |
| `VsaSharedDir` | `VsaPluginsDir` |
| `VsaUses` (a plugin layer uses other plugins) | `VsaPluginUses` |
| `ModuleMetadata.Shared`, `SliceRouter.Shared` | `ModuleMetadata.Plugins`, `SliceRouter.Plugins` |
| `ModuleRoot.IsShared` | `ModuleRoot.IsPlugin` |
| `tests/Fixtures/SharedApp`, `tests/Fixtures/Host/Shared` | `PluginApp`, `Host/Plugins` |

These names do not change: `Domain/Shared/`, `Domain.Shared`, `Slices/Shared/` and `VsaModuleUses`.

## Steps of the work

1. Change the names in VSA.Sdk, `SliceRouter`, `ModuleRoot`, `ModuleMetadata` and `LayerRules`.
2. Keep `Shared/` as a second root folder for one release, with a build warning `VSA0008`: "Move `Shared/<Module>` to `Plugins/<Module>`".
3. Rename the fixtures, the CLI templates and the docs. Run `dotnet run solution.cs`.
4. Move the folders in `vsa-consumer-dotnet` and `vsa-consumer-unity` after the release.

## Non-goals

- No plugins that load at run time. Reason: a plugin module is a compile-time base, and VSA.Sdk references it from each module.
- No optional plugins in this proposal. Each module gets each plugin module, as each module gets the framework layers.

## Open questions

- Is a base class `Plugin` necessary, or does a plugin root inherit `Module`, as a Shared root does?
- Is `Domain.Shared` also a candidate for a rename, for example `Domain.Public`?
