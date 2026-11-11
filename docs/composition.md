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
| `UseWebHost()` (VSA.Host) | ASP.NET Core: the HTTP API of the requests, and the views |
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
├─ VSA layer installers            events, commands, requests, in-process bus, presenters
├─ host.Install                    web: the request handlers to map, views
├─ Services(...) callbacks         app registrations
├─ Module.Configure                each module, last
└─ host.Build                      web: Build, map one route for each request, map the views
```

`Module.Configure` runs last, because a module can replace a default with `ModuleBuilder.Replace`. The order is inside `Build`, so an app cannot change it.

## Definitions and transports

A module defines its requests, commands, events, handlers and presenters one time. The same code works on every host. The host selects only the transports:

| Kind | Part | Generic host | Web host |
|---|---|---|---|
| Definition | Events (`IntegrationEventHandler<T>`), in-process publisher | Yes | Yes |
| Definition | Commands (`CommandHandler<,>`), in-process sender | Yes | Yes |
| Definition | Requests (`RequestHandler<,>`), `InProcessBus` (`IBus`) | Yes | Yes |
| Definition | Presenters, `LivePresenters` | Yes | Yes |
| Transport | HTTP API: one route for each request | No | Yes |
| Transport | Views (`ViewsRoot`): Razor components | No | Yes |

```
Presenter → IBus.Send(request) → InProcessBus → RequestHandler<,>       every host
API client → HTTP → route of the request → RequestHandler<,>            web host
```

A request gives its `Method` and `Route` as static members. Only the HTTP API reads them. On `netstandard2.1`, `IRoutedRequest` has no members (`#if NET`), so the same request compiles in Unity, and nothing reads the route there.

```csharp
public interface IRoutedRequest {
#if NET
    static abstract HttpMethod Method { get; }
    static abstract string Route { get; }
#endif
}
```

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

The module roots are in order: the plugin modules first, then by name. If the app has a views root, the views run in that process. Two views roots stop the build.

## Message installers have one shape

Each layer has one internal static installer in its own project. Only VSA.Composition and VSA.Host call them (`InternalsVisibleTo`). Each message installer does the same 3 steps:

| Step | Events | Commands | Requests |
|---|---|---|---|
| 1. Find | `IntegrationEventHandler<>` | `CommandHandler<,>` | `RequestHandler<,>` |
| 2. Check | None | One handler for each command | One handler for each request |
| 3. Register | Publisher, each handler, the default pipeline | Sender, each handler, the default pipeline | Each handler, the default pipeline |

`PipelineInstaller` (VSA.Handlers) runs first: it finds the validators and the Before and After events of the modules. Each send goes through a pipeline (`docs/handler-pipeline.md`).

```csharp
static class CommandHandlerInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var handlers = types.Implementations(typeof(CommandHandler<,>));
        EnsureOneHandlerForEachCommand(handlers);
        services.AddScoped<ICommandSender, InProcessCommandSender>();
        services.AddDefaultPipeline(
            PipelineKind.Commands, new PipelineBuilder<IMessage>().HandlersOf(typeof(CommandHandler<,>)));
        foreach (var (service, handler) in handlers) {
            services.AddScoped(service, handler);
        }
    }
}
```

`ModuleTypes.Implementations(typeof(TBase))` gives each concrete type of the modules that derives from `TBase`, with its service type. `ModuleTypes` reads the layer assemblies of the modules one time.

### Open generic types and keys

An open generic handler, validator or event is closed one time for each key. The installers get only closed types.

```text
open type   ChangeResourceHandler<T> : CommandHandler<ChangeResourceCommand<T>, int>  where T : struct, IResource
keys        each type of the modules with [Key]                                        Health, Mana, Gold
fit         the keys that meet each constraint of T                                    Health, Mana
closed      ChangeResourceHandler<Health>, ChangeResourceHandler<Mana>
```

```csharp
[Key]
public interface IResource;

public readonly record struct Health(int Value) : IResource;
public sealed record ChangeResourceCommand<T>(int Delta) : ICommand<int> where T : struct, IResource;

await commands.SendAsync(new ChangeResourceCommand<Health>(-2), ct);
```

| Rule | Detail |
|---|---|
| Key | A struct or a concrete class with `[Key]` (`VSA.Analyzers.Markers`) on the type, on a base class, or on an interface that it implements at any depth. |
| Closed | Only an open type that derives from the scanned base. Other generic classes of a module do not change. |
| Parameters | One generic parameter. An open handler with two or more parameters stops the start, with a message. |
| No key fits | The open type gets no closed type. A send of that command fails with "has no handler". |
| Services | `ModuleTypes` is a singleton service. A plugin with its own base, for example a primitive handler base, reads its closed types with `Implementations(typeof(TBase))`. |

The key source is `IKeySource`. The default is `ConstraintKeys`. To use a hand list or a generated list, give the app another source:

```csharp
await VsaBuilder.Create(args).Keys(new GeneratedKeys()).UseWebHost().Build().RunAsync();
```

## The HTTP API

The web host maps one route for each `RequestHandler<,>`, with ASP.NET Core minimal APIs (`MapMethods`).

| Step | Source |
|---|---|
| Bind the request | The JSON body (`POST`, `PUT`, `PATCH`), then the route values, then the query |
| Run the handler | The pipeline of the request, with the services of the scope of the HTTP request |
| Success | 200 with the response as JSON |
| Error of the handler | A problem response: 404 if the code ends with `.not_found`, else 400 |
| Value that does not bind | A problem response, 400, title `request.not_valid` |

## Projects

```
Module/              Module, Plugin, ModuleBuilder, ModuleRoot, ModuleTypes, AppScan, IKeySource, ConstraintKeys, KeyTypes
Handlers/Core/       PipelineBuilder, Pipelines, the runners, Validator, Before, After, PipelineInstaller
Handlers/Events/     EventHandlerInstaller, InProcessEventPublisher
Handlers/Commands/   CommandHandlerInstaller, InProcessCommandSender
Handlers/Requests/   RequestHandler, RequestHandlerInstaller
Presenters/          PresenterInstaller
Views/               ViewsRoot, ViewsInstaller
Composition/         VsaBuilder, IVsaHost, GenericVsaHost, VsaArchitecture, InProcessBus
Host/                UseWebHost, WebVsaHost, HttpApi, HttpRequestBinder
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
| `IBus` | `InProcessBus` | `InProcessBus` |
| `ILogger<T>` | Yes | Yes |
| `IConfiguration` | `appsettings.json`, environment, `args` | `appsettings.json`, environment, `args` |

## Unity target

`VSA.Composition` and all definition layers are portable layers (`VsaPortableLayer`): they build for `net10.0` and `netstandard2.1`. The transports (VSA.Host, VSA.Views) need ASP.NET Core, so they build for `net10.0` only.

The generic host uses `Microsoft.Extensions.Hosting` 8.0. A Unity app vendors it and its dependencies, with `VSA.Composition.dll`. The Unity app calls `StopAsync()` on the host when it quits, and `RunAsync()` then ends.

## Non-goals

- No VSA container interface. Reason: `IServiceCollection` and `IServiceProvider` are already container-neutral, and most containers read them.
- No VContainer bridge in the framework. Reason: only a Unity app with VContainer needs it, so the app keeps it.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- Remote bus: an `IBus` over HTTP, for a view or a Unity client in another process. It can use the same routes as the HTTP API.
- Generated module list: VSA.Sdk knows the module projects at build time. It can write their names in the host assembly, and `AppScan` can read that list first.
- Unity check: `AppDomain.GetAssemblies()` with IL2CPP, and `RunAsync` in an `async` `Start` method. Check both in `vsa-consumer-unity`.
- Architecture definition: `VsaBuilder.Create(definition, args)` can make `VsaArchitecture` public, for other architectures.
