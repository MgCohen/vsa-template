# Composition: VsaBuilder and its hosts

An app starts with one builder call: `VsaBuilder.Create(args)`, then a host, then `Build()`. The host type selects the split between web and Unity. All registrations go to `IServiceCollection`, so module code does not know which container the app uses.

## Entry point

```csharp
// Web: App/Host/Program.cs
await VsaBuilder.Create(args).UseWebHost().Build().RunAsync();

// Unity or console
await VsaBuilder.Create().UseGenericHost().Build().RunAsync();
```

| Step | Use |
|---|---|
| `Create(args)` | Makes the builder. `args` go to the configuration of the host |
| `UseWebHost()` (VSA.Host) | ASP.NET Core: request routes, loopback bus, views |
| `UseGenericHost()` (VSA.Composition) | The .NET Generic Host: no ASP.NET Core |
| `Modules(...)` | Optional. Gives the modules. With no call, `AppScan` finds them. Tests use it |
| `Services(...)` | Optional. App registrations, before the modules |
| `Build()` | Returns the standard `IHost`: `RunAsync`, `StartAsync`, `StopAsync`, `Services` |

If the app selects no host, `Build()` stops with the message "The app has no host. Select one before Build: UseWebHost() or UseGenericHost()."

## What Build does

```
VsaBuilder.Build()
├─ host.CreateBuilder(args)        WebApplicationBuilder, or HostApplicationBuilder
├─ AppScan.Modules()               only if the app gave no modules
├─ VSA layer installers            events, commands, presenters (net10.0)
├─ host.Install                    web: loopback bus, request routes, views
├─ Services(...) callbacks         app registrations
├─ Module.Configure                each module, last
└─ host.Build                      web: Build, UseFastEndpoints, map the views
```

`Module.Configure` runs last, because a module can replace a default with `ModuleBuilder.Replace`. The order is inside `Build`, so an app cannot change it.

## Layers and hosts

| Layer | Dispatch | Generic host | Web host |
|---|---|---|---|
| Events (`IntegrationEventHandler<T>`) | In-process publisher | Yes | Yes |
| Commands (`CommandHandler<,>`) | In-process sender | Yes | Yes |
| Presenters | `LivePresenters` | `net10.0` only (issue #44) | Yes |
| Requests (`RoutedHandler<,>`) | FastEndpoints routes, HTTP | No | Yes |
| Views (`ViewsRoot`) | Razor components | No | Yes |

The installers of the requests and the views need ASP.NET Core, so only the web host runs them. A Unity app keeps the events and the commands, and it has no routes.

## AppScan finds the parts of the app

`AppScan` reads 2 sources and keeps each assembly that references a given assembly:

| Source | Finds |
|---|---|
| The loaded assemblies | Unity: all script assemblies load when the domain starts |
| The `.dll` files in `AppContext.BaseDirectory` | .NET: a module assembly loads only when code uses one of its types |

- The scan reads the metadata of a file with `System.Reflection.Metadata`. It loads only the files that reference the assembly.
- A file that the runtime cannot load by name (not in `deps.json`) is not part of the app. The scan ignores it.
- Each source finds nothing where it does not apply, so the scan has no Unity case.

| Part | Scan for | Rule |
|---|---|---|
| Module root | A reference to `VSA.Module`, and the `VsaModule` metadata | One class that inherits `Module` (`ModuleRoot.CreateModule`) |
| Views root | A reference to `VSA.Views` | One component that inherits `ViewsRoot` |

The module roots are in order: the Shared modules first, then by name. If the app has a views root, the views run in that process. Two views roots stop the build.

## Message installers have one shape

Each layer has one internal static installer in its own project. Only VSA.Composition and VSA.Host call them (`InternalsVisibleTo`). Each message installer does the same 3 steps:

| Step | Events | Commands | Requests |
|---|---|---|---|
| 1. Find | `IntegrationEventHandler<>` | `CommandHandler<,>` | `RoutedHandler<,>` |
| 2. Check | None | One handler for each command | None. No handlers: stop |
| 3. Register | Publisher, each handler | Sender, each handler | FastEndpoints, for the handler assemblies |

```csharp
static class CommandHandlerInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var handlers = types.Implementations(typeof(CommandHandler<,>));
        EnsureOneHandlerForEachCommand(handlers);
        services.AddScoped<ICommandSender, InProcessCommandSender>();
        foreach (var (service, handler) in handlers) {
            services.AddScoped(service, handler);
        }
    }
}
```

`ModuleTypes.Implementations(typeof(TBase))` gives each concrete type of the modules that derives from `TBase`, with its service type. `ModuleTypes` reads the layer assemblies of the modules one time. FastEndpoints takes assemblies, not types, so the request installer gives the assemblies of the handlers.

## Projects

```
Module/              Module, ModuleBuilder, ModuleRoot, ModuleTypes, AppScan
Handlers/Events/     EventHandlerInstaller, InProcessEventPublisher
Handlers/Commands/   CommandHandlerInstaller, InProcessCommandSender
Handlers/Requests/   RequestEndpointInstaller
Presenters/          PresenterInstaller
Views/               ViewsRoot, ViewsInstaller
Composition/         VsaBuilder, IVsaHost, GenericVsaHost, VsaArchitecture
Host/                UseWebHost, WebVsaHost, HttpBus
```

- `VsaArchitecture` (internal) holds the VSA choices: how to find the modules, the layer installers and their order. `VsaBuilder` calls it.
- `IVsaHost` has 3 members: `CreateBuilder`, `Install` and `Build`. `GenericVsaHost` and `WebVsaHost` implement it.
- Each layer project references `VSA.Module` with `PrivateAssets="all"`. So a module project of that layer does not see `VSA.Module`.

## The container

Module code and installers use two shared interfaces from .NET:

| Interface | Job | Package |
|---|---|---|
| `IServiceCollection` | Registration: a list of `ServiceDescriptor` | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `IServiceProvider` | Resolution: `GetService(Type)` | `System` |

Both hosts build the Microsoft container. Other containers read `IServiceCollection` too:

| Container | Reads `IServiceCollection` |
|---|---|
| Autofac | `builder.Populate(services)` (`Autofac.Extensions.DependencyInjection`) |
| DryIoc, Lamar, SimpleInjector | Their own integration packages |
| VContainer | No support. The app needs a bridge |

A Unity app that uses VContainer writes its own bridge. The bridge copies each `ServiceDescriptor` into `IContainerBuilder`, and it wraps `IObjectResolver` as `IServiceProvider`.

## Module settings

`Module<TSettings>` binds `Modules:{Name}` to `TSettings` and checks it. Then it registers the instance as a singleton. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What each host gives

| Service | Web host | Generic host |
|---|---|---|
| `IBus` | `HttpBus` to the server address (loopback) | None. The app registers one with `Services(...)` |
| `ILogger<T>` | Yes | Yes |
| `IConfiguration` | `appsettings.json`, environment, `args` | `appsettings.json`, environment, `args` |

## Unity target

`VSA.Composition` is a Unity layer (`VsaUnityLayer`). On `netstandard2.1`, `Build` does not run `PresenterInstaller`, because `VSA.Contracts.Requests` uses `static abstract` members. Issue #44 tracks this.

The generic host uses `Microsoft.Extensions.Hosting` 8.0. A Unity app vendors it and its dependencies, with `VSA.Composition.dll`. The Unity app calls `StopAsync()` on the host when it quits, and `RunAsync()` then ends.

## Non-goals

- No VSA container interface. Reason: `IServiceCollection` and `IServiceProvider` are already container-neutral, and most containers read them.
- No VContainer bridge in the framework. Reason: only a Unity app with VContainer needs it, so the app keeps it.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- In-process bus: `HttpBus` is the only `IBus`. A request handler is a FastEndpoints endpoint, so a request needs HTTP.
- Generated module list: VSA.Sdk knows the module projects at build time. It can write their names in the host assembly, and `AppScan` can read that list first.
- Unity check: `AppDomain.GetAssemblies()` with IL2CPP, and `RunAsync` in an `async` `Start` method. Check both in `vsa-consumer-unity`.
- Architecture definition: `VsaBuilder.Create(definition, args)` can make `VsaArchitecture` public, for other architectures.
