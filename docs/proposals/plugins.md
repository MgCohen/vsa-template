# Proposal: rename Shared modules to Plugins

[Index](README.md)

Status: done, in the first of the four proposals. All four proposals ship in one release, so `Shared/` has no alias and no deprecation warning.

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
├─ Game.csproj, GamePlugin.cs          GamePlugin : Plugin
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
| `ModuleMetadata.Shared` | `ModuleMetadata.Plugins` |
| `ModuleRoot.IsShared` | `ModuleRoot.IsPlugin` |
| A root class that inherits `Module` | A root class that inherits `Plugin` or `Plugin<TSettings>` (`VSA1014`, and a startup check) |
| `tests/Fixtures/SharedApp`, `tests/Fixtures/Host/Shared` | `PluginApp`, `Host/Plugins` |
| No tool command | `vsa add plugin <Name>` |

These names do not change: `Domain/Shared/`, `Domain.Shared`, `Slices/Shared/`, `VsaModuleUses` and `SliceRouter.Shared` (the slice name of `Slices/Shared/`).

In the same change, the folder `framework/Layers/` becomes `framework/Core/`: the core is the base of each layer, and the plugin modules extend it. The package and assembly names (`VSA.Domain`, `VSA.Module`, ...) and the word "layer" do not change.

## Steps of the work

1. Change the names in VSA.Sdk, `SliceRouter`, `ModuleRoot`, `ModuleMetadata` and `LayerRules`. Add `Plugin`, `Plugin<TSettings>` and `VSA1014`.
2. Rename the fixtures, the CLI templates and the docs. Add `vsa add plugin`. Run `dotnet run solution.cs`.
3. Move `framework/Layers/` to `framework/Core/`.
4. Move the folders in `vsa-consumer-dotnet` and `vsa-consumer-unity` after the release.

## Non-goals

- No plugins that load at run time. Reason: a plugin module is a compile-time base, and VSA.Sdk references it from each module.
- No optional plugins in this proposal. Each module gets each plugin module, as each module gets the framework layers.

## Decisions

- A plugin root inherits `Plugin` (or `Plugin<TSettings>`), and `Plugin` inherits `Module`. The settings of a plugin module are in `Modules:{Name}`, as for a module.
- `Domain.Shared` keeps its name. After the rename, "Shared" means only the shared parts of one module: `Domain/Shared/` and `Slices/Shared/`.
