# Composition: the kernel, the app builder and the hosts

An app starts with one call on all hosts: `new MyApp().Run(args)`. The app class `MyApp : VsaApp<VsaDefinition>` uses `AppBuilder` (VSA.Kernel), and its `Host()` method selects the host. A console app can also use `AppBuilder.Create(args)`, then `Build()`. An app that builds its own container calls `services.AddVsa(args)`. All registrations go to `IServiceCollection`, so module code does not know which container the app uses.

## Entry point

```csharp
// Web: App/Host/Program.cs
return new MyApp().Run(args);

// Web: App/Host/MyApp.cs
public class MyApp : VsaApp<VsaDefinition> {
    protected override IAppHost Host() => new WebVsaHost();
}

// Console
await AppBuilder.Create(args).ScanFor(typeof(Module)).UseGenericHost().Build().RunAsync();

// Unity, or any app that owns its own start: no host
var services = new ServiceCollection().AddVsa(args).BuildServiceProvider();
```

| Step | Use |
|---|---|
| `Create(args)` | Makes the builder. `args` go to the configuration of the host |
| `UseWebHost()` (VSA.Host) | ASP.NET Core: the HTTP API of the requests, and the views |
| `UseGenericHost()` (VSA.Kernel) | The .NET Generic Host for a console app: no ASP.NET Core |
| No host call | `GenericAppHost` |
| `ScanFor(type)` | The anchor base that the scan finds. Default: `Anchor`. VSA: `Module` |
| `Anchors(...)`, `Modules(...)` | Optional. Gives the anchors. With no call, `AnchorScan` finds them. Tests use it |
| `Services(...)` | Optional. App registrations, before the modules |
| `Build()` | Returns the standard `IHost`: `RunAsync`, `StartAsync`, `StopAsync`, `Services` |

## The app class

`vsa new app <Name>` writes `App/Host/<Name>App.cs`. The app class is the one entry of the app: the runtime starts it, and the rule engine reads its rules (see `architecture-engine.md`). `VsaApp<TDefinition>` (VSA.Composition) inherits `App` (VSA.Kernel). Both are portable, so the same app class works on all hosts. `App` has the run, the host and the builder steps. `VsaApp` adds the definition (`Architecture`), `Modules()` and the scan for `Module`. `TDefinition` is the architecture definition of the app, for example `VsaDefinition` or `EngineDefinition`.

```
new MyApp().Run(args)                 in App
└─ Start(args)
   ├─ AppBuilder.Create(args)         with Host(), Keys(), Services, then Configure(builder): Modules() or ScanFor(Module)
   ├─ SelectedHost()                  the host of Host()
   ├─ Build()                         IHost
   └─ host.Run(app)                   web, generic: waits for the stop. Unity: returns
```

| Member | Default | Override to |
|---|---|---|
| `Start(string[] args)` | Builds the app, then the host runs it. Returns the exit code of the host | Change the run, or the exit code |
| `Build(string[] args)` | Builds the `IHost` from the builder steps | Change the build |
| `Host()` | `new GenericAppHost()` | Select the host: `WebVsaHost`, `ConsoleVsaHost`, or the Unity host |
| `Modules()` | `null`: `AnchorScan` finds the modules | Give the modules, for example in a test |
| `Keys()` | `new ConstraintKeys()` | Give the keys of the open module types |
| `Services(IServiceCollection)` | No registrations | Add app registrations |
| `Architecture` | `new TDefinition()` | Set the Unity folders: `UnityApp`, `UnitySource` (see `architecture-engine.md`) |

To add app rules, derive a definition from `VsaDefinition` and override `Rules` (see `architecture-engine.md`).

The instance members control the lifetime of the app:

| Member | Use |
|---|---|
| `Run(string[] args)` | Starts the app. A second `Run` while the app runs is an error |
| `Stop()` | Stops the app. If the app does not run, it does nothing |
| `IsRunning` | True from `Run` until the app stops |

After the app stops, `Run` can start it again. A host that waits (web, generic) returns from `Run` after `Stop`. A host that returns (Unity) keeps the app until `Stop`.

With no host, the app calls `AddVsa` on its own `IServiceCollection`:

| Call | Use |
|---|---|
| `services.AddVsa(args)` | Adds the VSA registrations to `services`. `AnchorScan` finds the modules |
| `services.AddVsa(args, vsa => ...)` | The same, with the builder steps `Modules(...)` and `Services(...)` |

If `services` already has an `IConfiguration` instance, `AddVsa` uses it. Else it reads `args`.

## The hosts

The app selects its host in `Host()`. The hosts are public classes:

| Host | Package | Use |
|---|---|---|
| `GenericAppHost` | VSA.Kernel | The default. The .NET Generic Host |
| `ConsoleVsaHost` | VSA.Composition | A console app with one entry command (`IConsoleCommand<TResult>`) |
| `WebVsaHost` | VSA.Host | ASP.NET Core: the HTTP API of the requests, and the views |
| The Unity host | `vsa-unity` | A Unity app. `Run` returns |

| `IAppHost` member | Use |
|---|---|
| `CreateBuilder`, `Install`, `Build` | Make the `IHost` |
| `Run(IHost app)` | Starts the app. The host selects if it waits for the stop or returns |
| `Stop(IHost app)` | Stops the app |

## What Build does

```
AppBuilder.Build()
├─ host.CreateBuilder(args)        WebApplicationBuilder, or HostApplicationBuilder
├─ AnchorScan.Anchors(base)        only if the app gave no anchors
├─ host.Install                    web: the request handlers to map, views
├─ Compose (the same as AddVsa)
│  ├─ IConfiguration, AddLogging   only if the host did not add them
│  ├─ AnchorScan.Installers()      each IInstaller: auto-register, pipeline, events, commands, requests, bus, presenters
│  ├─ Services(...) callbacks      app registrations
│  └─ Anchor.Configure             each anchor (module), last
└─ host.Build                      web: Build, map one route for each request, map the views
```

`Anchor.Configure` runs last, because a module can replace a default with `AnchorBuilder.Replace`. The order is inside `Compose`, so an app cannot change it. The installers have no order: each one adds its own services, and a test runs them in reverse order with the same result. `Build()` and `AddVsa` give the same VSA registrations.

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

## AnchorScan finds the parts of the app

`AnchorScan` (VSA.Kernel) reads 2 sources and keeps each loaded assembly that references a given assembly:

| Source | Finds |
|---|---|
| The loaded assemblies | Unity: all script assemblies load when the domain starts |
| The `.dll` files in `AppContext.BaseDirectory` | .NET: a module assembly loads only when code uses one of its types |

- The scan reads the metadata of a file with `System.Reflection.Metadata`. It loads only the files that reference the assembly.
- A file that the runtime cannot load by name (not in `deps.json`) is not part of the app. The scan ignores it.
- Each source finds nothing where it does not apply, so the scan has no Unity case.
- The scan keeps each loaded copy of an assembly. A copy in a different load context (for example the app that the rule engine checks) has other types, so the type checks drop it.

| Part | Scan for | Rule |
|---|---|---|
| Anchor root | A reference to the assembly of the anchor base (`VSA.Module` for `Module`) | One class that inherits the base in each namespace (`AnchorRoot.Create`) |
| Installer | A reference to `VSA.Kernel` | Each class that implements `IInstaller` |
| Views root | A reference to `VSA.Views` | One component that inherits `ViewsRoot` |

The anchors are in order of `Anchor.Order`, then by name: a plugin (`Order` 0) comes before a module (`Order` 1). If the app has a views root, the views run in that process. Two views roots stop the build.

## One discovery path

`AnchorTypes` finds the types of the anchors one time. Each installer reads the same result. It reads the root assembly and each layer assembly of a module, so a module that is one assembly (for example one Unity `.asmdef`) also works. Each send goes through a pipeline (`docs/handler-pipeline.md`).

```text
AnchorTypes.Registered()
├─ each concrete class with [AutoRegister]     closed class: as it is
│  (on the class, a base class or an interface) open class: closed for each type argument
└─ AutoRegisterInstaller                       registers each type and each marked base
   Command, request, event, presenter installers  add only the sender, the pipeline and the checks
```

| Marker | Side | Meaning |
|---|---|---|
| `[AutoRegister(lifetime)]`, `IAutoRegister` | Open or closed class | VSA registers each concrete class. Lifetime default: `Scoped`. |
| `[TypeArgument]`, `ITypeArgument` | Type argument | A struct or a concrete class that closes the open classes. |
| `[assembly: TypeArgument(typeof(int))]` | Type argument | A type that cannot have the attribute. |

The markers are in `VSA.Analyzers.Markers`. The same package has the stripping markers for IL2CPP. The UnityLinker finds them by class name, so do not rename them:

| Marker | Target | The linker keeps | VSA puts it on |
|---|---|---|---|
| `[RequireDerived]` | Class | Each derived class | The `[AutoRegister]` bases, `BeforeStep<>`, `AfterStep<>`, `Anchor`, `Module`, `App`, `VsaApp<TDefinition>` |
| `[RequireImplementors]` | Interface | Each implementor | `IAutoRegister`, `ITypeArgument`, `IAppHost`, `IInstaller`, `IRunner`, and the internal `IEnvelopeFactory`, `IBeforeCall`, `IAfterCall` |
| `[Preserve]` | Any | The marked member | The `[AutoRegister]` bases, `BeforeStep<>`, `AfterStep<>` |

The pipeline creates the runner, the envelope factory and the steps from a `Type` (`Activator`, `ActivatorUtilities`). The linker cannot see these uses, so each of their bases has a marker. `MarkedTypes` names the marker types with `typeof`, so VSA.Kernel uses the markers assembly in code.

An IL2CPP player with stripping High also needs a `link.xml` that keeps the markers assembly. Without it, the linker removes `[AutoRegister]` from the bases, and the scan finds no handlers. The UPM package must ship this file:

```xml
<linker>
  <assembly fullname="VSA.Analyzers.Markers" preserve="all" />
</linker>
```

The template bases have `[AutoRegister]`: `CommandHandler<,>`, `RequestHandler<,>`, `IntegrationEventHandler<>`, `Validator<>`, `Before<>`, `After<>` and `Presenter`. A class that derives from one of them needs no marker.

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

`AnchorTypes` is a singleton service. `Implementations(typeof(TBase))` gives the registered types that derive from `TBase`, with that base as the service. A plugin reads its own types with it.

The type argument source is `IKeySource`. The default is `ConstraintKeys`. To use a hand list or a generated list, give the app another source:

```csharp
await AppBuilder.Create(args).ScanFor(typeof(Module)).Keys(new GeneratedKeys()).UseWebHost().Build().RunAsync();
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
framework/Kernel/                      App, AppBuilder, Anchor, AnchorBuilder, IInstaller, AutoRegisterInstaller
  Hosting/                             IAppHost, GenericAppHost
  Scan/                                AnchorScan, AnchorRoot, AnchorTypes, IKeySource, ConstraintKeys, MarkedTypes
  Factories/                           IFactory, AddFactory
framework/Components/Messages/
  Handlers/                            PipelineBuilder, Pipelines, the runners, Validator, Before, After, PipelineInstaller
framework/Architectures/Vsa/
  Module/                              Module, Plugin, Module<TSettings>, Plugin<TSettings>
  Handlers/Events/                     EventHandlerInstaller, InProcessEventPublisher
  Handlers/Commands/                   CommandHandlerInstaller, InProcessCommandSender
  Handlers/Requests/                   RequestHandler, RequestHandlerInstaller
  Presenters/                          PresenterInstaller
  Views/                               ViewsRoot, ViewsInstaller
  Composition/                         VsaApp, the Modules and UseConsoleHost extensions, BusInstaller, InProcessBus
    Hosts/Console/                     ConsoleVsaHost, ConsoleEntry, ConsoleExit
  Host/                                UseWebHost, WebVsaHost, HttpApi, HttpRequestBinder
```

- The kernel knows no layer. Each layer project has one `IInstaller`, and the kernel finds it with `AnchorScan.Installers()`.
- `IAppHost` has 3 members to make the `IHost`: `CreateBuilder`, `Install` and `Build`. `GenericAppHost`, `ConsoleVsaHost` and `WebVsaHost` implement it.

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

`Module<TSettings>` binds `{Section}:{Name}` to `TSettings` and checks it. The section of a module is `Modules` (`Anchor.Section`). Then it registers the instance as a singleton. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What each host gives

| Service | Web host | Generic host | `AddVsa` (no host) |
|---|---|---|---|
| `IBus` | `InProcessBus` | `InProcessBus` | `InProcessBus` |
| `ILogger<T>` | Yes, host providers | Yes, host providers | Yes, no providers. The app adds them |
| `IConfiguration` | `appsettings.json`, environment, `args` | `appsettings.json`, environment, `args` | The one the app gives, or `args` |
| Lifetime, environment, `IHostedService` start | Yes | Yes | None |

## Unity target

`VSA.Kernel`, `VSA.Composition`, `VSA.Architecture`, `VSA.Architecture.Vsa` and all definition layers are portable layers (`VsaPortableLayer`): they build for `net10.0` and `netstandard2.1`. The transports (VSA.Host, VSA.Views) need ASP.NET Core, so they build for `net10.0` only.

A Unity app calls `new MyApp().Run(args)` from the `VsaHost` component of the UPM package (repo `vsa-unity`). The Unity host of that package returns from `Run`, and it calls `Stop` when the app quits or the play mode stops. An app that owns its container calls `AddVsa` and builds the container, as below. The default .NET host expects an app that owns its process, and Unity owns the process. In Unity, the default host fails: the EventLog logger provider throws on Mono, and an empty host has the content root `Unity.exe\`.

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
- No Unity code in this repository (the Unity host, `UnityLogProvider`, a view base). Reason: it needs `UnityEngine`. The UPM package (repo `vsa-unity`) keeps it. A Unity app returns the Unity host from `Host()`.
- No view injector and no scene scan. Reason: a factory gives each object its services, and a Unity app often has its own container for scene objects.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- Remote bus: an `IBus` over HTTP, for a view or a Unity client in another process. It can use the same routes as the HTTP API.
- Generated module list: VSA.Sdk knows the module projects at build time. It can write their names in the host assembly, and `AnchorScan` can read that list first.
- IL2CPP check: the Unity check runs on Mono only. Run `AnchorScan` and `AddVsa` in an IL2CPP player build of `vsa-consumer-unity`.
- Hosting DLLs in Unity: VSA.Kernel references `Microsoft.Extensions.Hosting` for `GenericAppHost`, so a Unity app still vendors it. The reference can move to the `net10.0` target only.
