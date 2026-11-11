# Composition: VsaBuilder and its hosts

An app starts with one call. Web and console apps use `VsaBuilder.Create(args)`, then a host and `Build()`. An app with no host, for example a Unity app, calls `services.AddVsa(args)`. All registrations go to `IServiceCollection`, so module code does not know which container the app uses.

## Entry point

```csharp
// Web: App/Host/Program.cs
await VsaBuilder.Create(args).UseWebHost().Build().RunAsync();

// Console
await VsaBuilder.Create(args).UseGenericHost().Build().RunAsync();

// Unity, or any app that owns its own start: no host
var services = new ServiceCollection().AddVsa(args).BuildServiceProvider();
```

| Step | Use |
|---|---|
| `Create(args)` | Makes the builder. `args` go to the configuration of the host |
| `UseWebHost()` (VSA.Host) | ASP.NET Core: the HTTP API of the requests, and the views |
| `UseGenericHost()` (VSA.Composition) | The .NET Generic Host for a console app: no ASP.NET Core |
| `Modules(...)` | Optional. Gives the modules. With no call, `AppScan` finds them. Tests use it |
| `Services(...)` | Optional. App registrations, before the modules |
| `Build()` | Returns the standard `IHost`: `RunAsync`, `StartAsync`, `StopAsync`, `Services` |

With no host, the app calls `AddVsa` on its own `IServiceCollection`:

| Call | Use |
|---|---|
| `services.AddVsa(args)` | Adds the VSA registrations to `services`. `AppScan` finds the modules |
| `services.AddVsa(args, vsa => ...)` | The same, with the builder steps `Modules(...)` and `Services(...)` |

If `services` already has an `IConfiguration` instance, `AddVsa` uses it. Else it reads `args`.

If the app selects no host, `Build()` stops with the message "The app has no host. Select one before Build: UseWebHost() or UseGenericHost(). Or call services.AddVsa(args) with no host."

## What Build does

```
VsaBuilder.Build()
├─ host.CreateBuilder(args)        WebApplicationBuilder, or HostApplicationBuilder
├─ AppScan.Modules()               only if the app gave no modules
├─ host.Install                    web: the request handlers to map, views
├─ Compose (the same as AddVsa)
│  ├─ IConfiguration, AddLogging   only if the host did not add them
│  ├─ VSA layer installers         events, commands, requests, in-process bus, presenters, module types
│  ├─ Services(...) callbacks      app registrations
│  └─ Module.Configure             each module, last
└─ host.Build                      web: Build, map one route for each request, map the views
```

`Module.Configure` runs last, because a module can replace a default with `ModuleBuilder.Replace`. The order is inside `Compose` (internal), so an app cannot change it. `Build()` and `AddVsa` give the same VSA registrations.

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

## One discovery path

`ModuleTypes` finds the types of the modules one time. Each installer reads the same result. Each send goes through a pipeline (`docs/handler-pipeline.md`).

```text
ModuleTypes.Registered()
├─ each concrete class with [AutoRegister]     closed class: as it is
│  (on the class, a base class or an interface) open class: closed for each type argument
└─ ModuleTypeInstaller                         registers each type and each marked base
   Command, request, event, presenter installers  add only the sender, the pipeline and the checks
```

| Marker | Side | Meaning |
|---|---|---|
| `[AutoRegister(lifetime)]`, `IAutoRegister` | Open or closed class | VSA registers each concrete class. Lifetime default: `Scoped`. |
| `[TypeArgument]`, `ITypeArgument` | Type argument | A struct or a concrete class that closes the open classes. |
| `[assembly: TypeArgument(typeof(int))]` | Type argument | A type that cannot have the attribute. |

The markers are in `VSA.Analyzers.Markers`. The template bases have `[AutoRegister]`: `CommandHandler<,>`, `RequestHandler<,>`, `IntegrationEventHandler<>`, `Validator<>`, `Before<>`, `After<>` and `Presenter`. A class that derives from one of them needs no marker.

```csharp
[TypeArgument]
public interface IResource;

public readonly record struct Health(int Value) : IResource;

sealed class ChangeResourceHandler<T> : CommandHandler<ChangeResourceCommand<T>, int>
    where T : struct, IResource { ... }                     // closed for Health, Mana

public interface IDamageRule : IAutoRegister;
sealed class DamageRule<T> : IDamageRule where T : struct, IWeapon { ... }   // closed for Sword, Dagger

[AutoRegister(Lifetime.Singleton)]
sealed class WeaponCache<T> where T : struct, IWeapon { ... }
```

### Rules

| Rule | Detail |
|---|---|
| Registration | The class itself, and each base class or interface with the marker, except `IAutoRegister`. |
| One instance | Each marked base resolves the class itself, so a scope has one instance. |
| Type arguments | Only marked types and assembly types. A type without the marker is never a type argument. |
| Constraints | `class`, `struct`, `unmanaged` and each type constraint. A struct with a reference field does not meet `unmanaged`. |
| Parameters | One generic parameter. An open class with two or more parameters stops the start, with a message. |
| No marker | VSA does not close or register the class. |
| No type argument fits | The open class gets no closed type. A send of that command fails with "has no handler". |
| Owner | An installer can register a class first, for example a presenter with a factory. The installer step does not replace it. |

`ModuleTypes` is a singleton service. `Implementations(typeof(TBase))` gives the registered types that derive from `TBase`, with that base as the service. A plugin reads its own types with it.

The type argument source is `IKeySource`. The default is `ConstraintKeys`. To use a hand list or a generated list, give the app another source:

```csharp
await VsaBuilder.Create(args).Keys(new GeneratedKeys()).UseWebHost().Build().RunAsync();
```

### Checks

The checks read the registered types. They do not find types.

| Check | Rule |
|---|---|
| Commands | One handler for each command |
| Requests | One handler for each request |

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
Module/              Module, Plugin, ModuleBuilder, ModuleRoot, ModuleTypes, ModuleTypeInstaller, AppScan, IKeySource, ConstraintKeys, MarkedTypes
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

A Unity app that uses VContainer builds the Microsoft container from `AddVsa`, then routes to it. For each VSA service type, VContainer gets a registration that resolves the service from the Microsoft container. A singleton comes from the root. A scoped or transient service comes from one Microsoft scope for each VContainer scope. So VSA keeps the Microsoft semantics, and Unity classes get VSA services by constructor injection.

A copy of each `ServiceDescriptor` into `IContainerBuilder` does not work. VContainer refuses two registrations of one implementation type, and VSA registers several instances of one type and reads them as a list (`AddPipeline`). The spike is on the branch `spike/compose-containers` of `vsa-consumer-unity`.

## Factories

A factory makes objects that the container cannot make, for example Unity objects from a prefab. The factory gets its services, and it gives the made object what it needs. Only the composition root and the factories resolve services. The made objects do not get `IServiceProvider`.

| Type | Use |
|---|---|
| `IFactory<T>` | `T Create()` |
| `IFactory<TArg, T>` | `T Create(TArg arg)` |
| `services.AddFactory<TFactory>()` | A custom factory. It gets its services from its constructor. It is registered as each `IFactory` that it implements |
| `services.AddFactory<T, TService>(create)` | A simple factory: a function of one service. Also with two services |

```csharp
public sealed class EnemyFactory(IBus bus, EnemySettings settings) : IFactory<Vector3, Enemy> {
    public Enemy Create(Vector3 at) {
        var enemy = Object.Instantiate(settings.Prefab, at, Quaternion.identity);
        enemy.Bind(bus);
        return enemy;
    }
}

services.AddFactory<EnemyFactory>();
services.AddFactory<CounterLabel, CounterPresenter>(presenter => {
    var label = Object.Instantiate(labelPrefab);
    label.Bind(presenter);
    return label;
});
```

A simple factory resolves its services on each `Create`, from the scope that resolved the factory. A factory knows no view and no presenter: views, enemies and other objects use the same factories.

## Module settings

`Module<TSettings>` binds `Modules:{Name}` to `TSettings` and checks it. Then it registers the instance as a singleton. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What each host gives

| Service | Web host | Generic host | `AddVsa` (no host) |
|---|---|---|---|
| `IBus` | `InProcessBus` | `InProcessBus` | `InProcessBus` |
| `ILogger<T>` | Yes, host providers | Yes, host providers | Yes, no providers. The app adds them |
| `IConfiguration` | `appsettings.json`, environment, `args` | `appsettings.json`, environment, `args` | The one the app gives, or `args` |
| Lifetime, environment, `IHostedService` start | Yes | Yes | None |

## Unity target

`VSA.Composition` and all definition layers are portable layers (`VsaPortableLayer`): they build for `net10.0` and `netstandard2.1`. The transports (VSA.Host, VSA.Views) need ASP.NET Core, so they build for `net10.0` only.

A Unity app calls `AddVsa` and builds the container. It does not use a host. The default .NET host expects an app that owns its process, and Unity owns the process. In Unity, the default host fails: the EventLog logger provider throws on Mono, and an empty host has the content root `Unity.exe\`.

```csharp
public sealed class AppRoot : MonoBehaviour {
    ServiceProvider services;

    public IServiceProvider Services => services;

    void Awake() {
        services = new ServiceCollection()
            .AddLogging(l => l.AddProvider(new UnityLogProvider()))
            .AddVsa("--Modules:Counter:Max=5")
            .BuildServiceProvider();
    }

    void OnDestroy() => services.Dispose();
}
```

The app selects the entry point: a component, a VContainer `LifetimeScope` or a test. `UnityLogProvider` is app code that calls `Debug.Log`. The Unity check runs the EditMode tests of `vsa-consumer-unity` (Mono) against a local pack.

## Non-goals

- No VSA container interface. Reason: `IServiceCollection` and `IServiceProvider` are already container-neutral, and most containers read them.
- No VContainer bridge in the framework. Reason: only a Unity app with VContainer needs it, so the app keeps it. Use the router, not a copy of the registrations.
- No Unity host. Reason: Unity needs the VSA registrations, not a host. `AddVsa` gives them.
- No Unity code in the framework (`UnityLogProvider`, a VContainer bridge, a view base). Reason: only Unity apps need it, and VContainer is not on NuGet. The Unity app keeps it. A UPM package can come later, when a second Unity app needs it.
- No view injector and no scene scan. Reason: a factory gives each object its services, and a Unity app often has its own container for scene objects.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- Remote bus: an `IBus` over HTTP, for a view or a Unity client in another process. It can use the same routes as the HTTP API.
- Generated module list: VSA.Sdk knows the module projects at build time. It can write their names in the host assembly, and `AppScan` can read that list first.
- IL2CPP check: the Unity check runs on Mono only. Run `AppScan` and `AddVsa` in an IL2CPP player build of `vsa-consumer-unity`.
- Hosting DLLs in Unity: VSA.Composition references `Microsoft.Extensions.Hosting` for `GenericVsaHost`, so a Unity app still vendors it. The reference can move to the `net10.0` target only.
- Architecture definition: `VsaBuilder.Create(definition, args)` can make `VsaArchitecture` public, for other architectures.
