# Composition: VsaBuilder and its hosts

An app starts with one builder call: `VsaBuilder.Create(args)`, then a host and `Build()`, or `Compose(services)` with no host. Web and console apps use a host. A Unity app uses `Compose`. All registrations go to `IServiceCollection`, so module code does not know which container the app uses.

## Entry point

```csharp
// Web: App/Host/Program.cs
await VsaBuilder.Create(args).UseWebHost().Build().RunAsync();

// Console
await VsaBuilder.Create(args).UseGenericHost().Build().RunAsync();

// Unity, or any app that owns its own start: no host
var services = VsaBuilder.Create().Compose(new ServiceCollection()).BuildServiceProvider();
```

| Step | Use |
|---|---|
| `Create(args)` | Makes the builder. `args` go to the configuration of the host |
| `UseWebHost()` (VSA.Host) | ASP.NET Core: the HTTP API of the requests, and the views |
| `UseGenericHost()` (VSA.Composition) | The .NET Generic Host for a console app: no ASP.NET Core |
| `Modules(...)` | Optional. Gives the modules. With no call, `AppScan` finds them. Tests use it |
| `Services(...)` | Optional. App registrations, before the modules |
| `Build()` | Returns the standard `IHost`: `RunAsync`, `StartAsync`, `StopAsync`, `Services` |
| `Compose(services, configuration)` | No host. Adds the VSA registrations to `services` and returns it. With no `configuration`, it reads `args` |

If the app selects no host, `Build()` stops with the message "The app has no host. Select one before Build: UseWebHost() or UseGenericHost(). Or call Compose(services) with no host."

## What Build does

```
VsaBuilder.Build()
├─ host.CreateBuilder(args)        WebApplicationBuilder, or HostApplicationBuilder
├─ AppScan.Modules()               only if the app gave no modules
├─ host.Install                    web: the request handlers to map, views
├─ Compose(services, configuration)
│  ├─ IConfiguration, AddLogging   only if the host did not add them
│  ├─ VSA layer installers         events, commands, requests, in-process bus, presenters
│  ├─ Services(...) callbacks      app registrations
│  └─ Module.Configure             each module, last
└─ host.Build                      web: Build, map one route for each request, map the views
```

`Module.Configure` runs last, because a module can replace a default with `ModuleBuilder.Replace`. The order is inside `Compose`, so an app cannot change it. `Build()` and `Compose` give the same VSA registrations.

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
Module/              Module, Plugin, ModuleBuilder, ModuleRoot, ModuleTypes, AppScan
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

`Build()` builds the Microsoft container. After `Compose`, the app selects the container. Other containers read `IServiceCollection` too:

| Container | Reads `IServiceCollection` |
|---|---|
| Autofac | `builder.Populate(services)` (`Autofac.Extensions.DependencyInjection`) |
| DryIoc, Lamar, SimpleInjector | Their own integration packages |
| VContainer | No support. The app routes to the Microsoft container |

A Unity app that uses VContainer builds the Microsoft container from `Compose`, then routes to it. For each VSA service type, VContainer gets a registration that resolves the service from the Microsoft container. A singleton comes from the root. A scoped or transient service comes from one Microsoft scope for each VContainer scope. So VSA keeps the Microsoft semantics, and Unity classes get VSA services by constructor injection.

A copy of each `ServiceDescriptor` into `IContainerBuilder` does not work. VContainer refuses two registrations of one implementation type, and VSA registers several instances of one type and reads them as a list (`AddPipeline`). The spike is on the branch `spike/compose-containers` of `vsa-consumer-unity`.

## Module settings

`Module<TSettings>` binds `Modules:{Name}` to `TSettings` and checks it. Then it registers the instance as a singleton. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What each host gives

| Service | Web host | Generic host | `Compose` (no host) |
|---|---|---|---|
| `IBus` | `InProcessBus` | `InProcessBus` | `InProcessBus` |
| `ILogger<T>` | Yes, host providers | Yes, host providers | Yes, no providers. The app adds them |
| `IConfiguration` | `appsettings.json`, environment, `args` | `appsettings.json`, environment, `args` | The one the app gives, or `args` |
| Lifetime, environment, `IHostedService` start | Yes | Yes | None |

## Unity target

`VSA.Composition` and all definition layers are portable layers (`VsaPortableLayer`): they build for `net10.0` and `netstandard2.1`. The transports (VSA.Host, VSA.Views) need ASP.NET Core, so they build for `net10.0` only.

A Unity app calls `Compose` and builds the container. It does not use a host. The default .NET host expects an app that owns its process, and Unity owns the process. In Unity, the default host fails: the EventLog logger provider throws on Mono, and an empty host has the content root `Unity.exe\`.

```csharp
public sealed class AppRoot : MonoBehaviour {
    ServiceProvider services;

    public IServiceProvider Services => services;

    void Awake() {
        services = VsaBuilder.Create("--Modules:Counter:Max=5")
            .Services(s => s.AddLogging(l => l.AddProvider(new UnityLogProvider())))
            .Compose(new ServiceCollection())
            .BuildServiceProvider();
    }

    void OnDestroy() => services.Dispose();
}
```

The app selects the entry point: a component, a VContainer `LifetimeScope` or a test. `UnityLogProvider` is app code that calls `Debug.Log`. The Unity check runs the EditMode tests of `vsa-consumer-unity` (Mono) against a local pack.

## Non-goals

- No VSA container interface. Reason: `IServiceCollection` and `IServiceProvider` are already container-neutral, and most containers read them.
- No VContainer bridge in the framework. Reason: only a Unity app with VContainer needs it, so the app keeps it. Use the router, not a copy of the registrations.
- No Unity host. Reason: Unity needs the VSA registrations, not a host. `Compose` gives them.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- Remote bus: an `IBus` over HTTP, for a view or a Unity client in another process. It can use the same routes as the HTTP API.
- Generated module list: VSA.Sdk knows the module projects at build time. It can write their names in the host assembly, and `AppScan` can read that list first.
- IL2CPP check: the Unity check runs on Mono only. Run `AppScan` and `Compose` in an IL2CPP player build of `vsa-consumer-unity`.
- Hosting DLLs in Unity: VSA.Composition references `Microsoft.Extensions.Hosting` for `GenericVsaHost`, so a Unity app still vendors it. The reference can move to the `net10.0` target only.
- Architecture definition: `VsaBuilder.Create(definition, args)` can make `VsaArchitecture` public, for other architectures.
