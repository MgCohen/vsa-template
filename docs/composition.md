# Composition: the app, the pack, the kernel and the hosts

An app starts with one call on all hosts: `new MyApp().Run(args)`. The app class selects one pack. The pack joins the architecture (for the rule engine), the kernel (the install of the architecture) and the host (the platform). All registrations go to `IServiceCollection`, so module code does not know which container the app uses.

## Startup shape

```
App.Run(args)
├─ Start(args)
│  ├─ host = Pack.CreateHost()
│  ├─ Services = host.Build(args, this)
│  │  └─ host.Compose(services, configuration, Pack.Kernel, types)
│  ├─ host.Start(Services, AppStop)          the host work: operation, web, signals
│  ├─ State = Running
│  └─ OnStart()
├─ wait for a stop request
└─ the stop
   ├─ State = Stopping
   ├─ OnStop()
   ├─ host.Dispose()
   └─ State = Stopped
```

The calls go one way. The app calls `Build`, `Start` and `Dispose` on the host. The host calls only `Compose` and `AppStop.Request`.

```csharp
// App/Program.cs
return new ShopApp().Run(args);

// App/ShopApp.cs: the web host and the VSA rules
public class ShopApp : App<Vsa<WebHost>>;

// The console host and an architecture of the app
public class ToolApp : App<Vsa<ToolArchitecture, ConsoleHost>>;

// Custom root services: one host class
public sealed class ShopHost : WebHost {
    protected override void Configure(IServiceCollection services, AppTypes types) =>
        services.AddSingleton<ShopClock>();
}
public class ShopApp : App<Vsa<ShopHost>>;
```

The app class file imports `Umpire.Kernels` (`App<TPack>`), `Umpire.Vsa` (`Vsa<THost>`) and the namespace of the host (`Umpire.Hosts.Web` or `Umpire.Hosts.Console`). The concept record `App` is in `Umpire.Architecture.Blocks`, and the app class file does not import that namespace. So `App` in the app class file is the class `Umpire.Kernels.App`.

## The kernel types

| Type | Package | Job |
|---|---|---|
| `IApp` | Umpire.Kernels | `State`, `Services`, `Start`, `Stop`, `Run`. A shell makes the app class through it |
| `App`, `App<TPack>` | Umpire.Kernels | The lifecycle of the app. `App<TPack>` makes its pack with `new TPack()` |
| `Pack`, `Pack<TArchitecture, TKernel, THost>` | Umpire.Kernels | Gives `Architecture`, `Kernel` and `CreateHost()` |
| `Kernel` | Umpire.Kernels | The install point of an architecture: the public `Install` calls the protected `Configure` |
| `Core` | Umpire.Kernels | The kernel of the core services: `IConfiguration`, logging, `AppTypes`, `AppStop`, `[AutoRegister]` |
| `AppHost` | Umpire.Kernels | The platform: `Build`, `Start`, `Dispose`, `Compose` (fixed), `Configure` |
| `AppState` | Umpire.Kernels | `Stopped`, `Running` or `Stopping` |
| `AppStop` | Umpire.Kernels | A singleton service. `Request(exitCode)` stops the app. The first request gives the exit code |
| `Operation` | Umpire.Kernels | A request that a host exposes: `Group`, `Name`, `Effect`, `Input`, `Invoke` |
| `Module`, `Module<TSettings>` | Umpire.Kernels | The module class at the top of a module folder: the public `Install` and the protected `Configure`. `Order` 1 |
| `AppTypes` | Umpire.Kernels | The one type source: the modules and their types |

The VSA pack adds `Vsa<THost>` (the VSA rules) and `Vsa<TArchitecture, THost>` (the rules of the app) in Umpire.Vsa. It adds `VsaKernel` (`packs/Vsa/Kernel/VsaKernel.cs`), `Plugin` and `Plugin<TSettings>` (the module class of a plugin module, `Order` 0) in Umpire.Vsa.Composition. Each type argument is a class with `new()`, so IL2CPP keeps one shared generic code.

## The app class

`umpire new app <Name>` writes `App/<Name>App.cs`. The app class is the one entry of the app: the runtime starts it, and the rule engine reads `Pack.Architecture` from it (see `architecture-engine.md`). The app class has the lifecycle only. It has no install point.

| Member | Default | Override to |
|---|---|---|
| `OnStart()` | Nothing | Run code after the build. `Services` is set |
| `OnStop()` | Nothing | Run code before the host disposes the services |

To add app rules, derive an architecture from `VsaArchitecture`, override `Rules`, and use `Vsa<TArchitecture, THost>` (see `architecture-engine.md`).

| Lifecycle member | Use |
|---|---|
| `Start(string[] args)` | Builds a new host, starts the host work, calls `OnStart`. Returns |
| `Stop()` | Stops the app and returns when `State` is `Stopped` |
| `Run(string[] args)` | `Start`, then waits until `State` is `Stopped`. Gives the exit code |
| `State` | `Running` from the start of the host work until the stop |
| `Services` | The services of the running app, else `null` |

A second `Start` or `Run` while the app runs is an error. If the app does not run, `Stop` does nothing. Each `Start` makes a new host, so `Start` after `Stop` starts the app again in the same process.

### One stop path

The app has two stop entries. `App.Stop()` is for the code that owns the app: a Unity shell, `Program.cs`, a test. `AppStop.Request(code)` is for the code inside the app: a module, a handler, a service. Both entries go to the same stop.

```text
any source ──► AppStop.Request(code)
                 └─► App posts the stop to the app thread
                       └─► OnStop ─► host.Dispose ─► State = Stopped
App.Stop() on the app thread ──► the stop runs inline: no post, no wait
App.Stop() on another thread ──► request, then wait until Stopped
Run ──► the stop runs on the thread of Run, after the wait
```

| App thread | Where the stop runs |
|---|---|
| `Start` on a thread with a `SynchronizationContext` | That context. `Start` keeps `SynchronizationContext.Current` |
| `Start` on a thread with no context | The thread pool |
| `Run` | The thread of `Run` |

- The inline rule prevents a deadlock. `Stop()` on the Unity main thread does not wait for work that it posted to the main thread.
- A stop request during `Start`, for example in `OnStart`, runs after `Start` ends.
- The first request gives the exit code. The app ignores a request of an earlier start.
- `Stop()` in host work only requests the stop, and returns. The stop waits for the host work, so a wait in `Stop()` is a deadlock. Host work is the console operation and each HTTP request of `WebHost`. Code inside the app uses `AppStop.Request`.
- `ConsoleHost` runs the operation on the thread pool, so the operation does not continue on the app thread.
- If `OnStop` or `host.Dispose` throws, `Stop()` gives the error to its caller. A stop on the thread pool also writes the error to the app log. A stop on an app thread gives the error to its `SynchronizationContext`.
- If the console operation cancels itself, the exit code is 1. A cancel from the stop is not an error.

| Stop source | Exit code of `Run` |
|---|---|
| `App.Stop()` | 0 |
| `AppStop.Request(code)`, from app code | `code` |
| The end of the console operation | The exit code of the response |
| Ctrl+C, or SIGTERM (console and web hosts) | 0 |
| `IHostApplicationLifetime.StopApplication()` (web host) | 0 |

Code inside the app uses the same call in all hosts:

```csharp
sealed class QuitHandler(AppStop stop) : CommandHandler<QuitCommand, Done> {
    public override Task<Result<Done>> HandleAsync(IEnvelope<QuitCommand> envelope) {
        stop.Request(envelope.Message.ExitCode);
        return Task.FromResult<Result<Done>>(new Done());
    }
}
```

## The hosts

A host is the platform. It builds the services and starts the host work. It does not wait. A host class is also the one place for the custom root services of an app (`Configure`).

| Host | Package | Use |
|---|---|---|
| `AppHost` | Umpire.Kernels | A `ServiceProvider` with the command-line configuration. Exposes no operation |
| `ConsoleHost` | Umpire.Hosts.Console | Command-line configuration. Runs one request, then `AppStop.Request(exitCode)` |
| `WebHost` | Umpire.Hosts.Web | ASP.NET Core: one HTTP route for each operation, and the views |
| `UnityHost` | `vsa-unity` | A Unity app. Its `Configure` adds the Unity log provider and the main thread `Dispatch` |

| `AppHost` member | Kind | Default |
|---|---|---|
| `Build(args, app)` | Virtual | `ServiceCollection`, `Compose` with `Configuration(args)`, `BuildServiceProvider`. Composes only |
| `Configuration(args)` | Virtual | `AddCommandLine` with the Umpire options only: `--Key=Value` and `--Key Value`. The host ignores other arguments |
| `Start(services, stop)` | Virtual | Nothing. Starts the host work, and sends each stop source to `stop.Request` |
| `Dispose()` | Virtual | Disposes the provider |
| `Compose(services, configuration, kernel, types)` | Fixed | See "What Compose does" |
| `Configure(services, types)` | Virtual | Nothing. The custom root services, last |
| `Modules` | `init` property | `null`: `AppTypes` finds the modules. A test gives a list |

- A host reads the arguments in `Configuration(args)`. The base host reads only `--Key=Value` and `--Key Value`, and it ignores other arguments, for example `-batchmode`, `-logFile path` or a path. So the raw arguments of a Unity player do not stop the start. `ConsoleHost` overrides it: it gives only the arguments from the first option to the base, so the words that select the request are not values.
- `ConsoleHost.Build` calls the base `Build`, then selects the request from the words before the first option. `ConsoleHost.Start` starts the request and listens for Ctrl+C and SIGTERM.
- `WebHost.Build` gives `args` to `WebApplication.CreateBuilder`, and ASP.NET Core reads them. It builds the `WebApplication` and maps the operations and the views. `WebHost.Start` calls `StartAsync`, connects `ApplicationStopping` to `AppStop`, and listens for Ctrl+C and SIGTERM. `WebHost.Dispose` calls `StopAsync`.
- `WebHost` turns off the native console lifetime, so only the stop path of the app reacts to Ctrl+C.
- `AppHost` does no OS work, so a Unity app does not listen for Ctrl+C.
- Each host has a public constructor with no parameters.

## What Compose does

```
AppHost.Compose(services, configuration, kernel, types)    fixed order
├─ new Core(configuration).Install(services, types)        Umpire.Kernels
│  ├─ IConfiguration                                        keep one that exists, else the given one
│  ├─ AddLogging                                            no log provider
│  ├─ AppTypes, AppStop                                     singletons
│  └─ auto-register                                         each [AutoRegister] type
├─ kernel.Install(services, types)                          calls Configure(services, types). VsaKernel:
│  ├─ AddRequestHandlers                                    each kind adds its sender and calls AddPipeline
│  ├─ AddEventHandlers, AddCommandHandlers
│  ├─ AddPresenters
│  └─ AddRequestOperations                                  one Operation for each request handler
├─ settings = new ModuleSettings(Settings(types))           the providers of the host
├─ module.Install(services, settings)                       each module, in Order, then by name
│  ├─ Module                                                Configure(services)
│  └─ Module<TSettings>, Plugin<TSettings>                  get the settings, then Configure(services, settings)
├─ CloseOpenDescriptors(services, types)                   closed descriptors for each open one (see "Open generics")
└─ host.Configure(services, types)                         last
```

The order is inside `Compose`, so a host cannot change it. A module replaces a default with `services.Replace<TService, TImplementation>()` (Umpire.Kernels). A pack variant kernel derives from `VsaKernel`, overrides the protected `Configure` and calls `base.Configure` first. An app that owns its container calls `Compose` with its own services, configuration, kernel and `AppTypes`.

## Definitions and transports

A module defines its requests, commands, events, handlers and presenters one time. The same code works on every host. The host selects only the transports:

| Kind | Part | `AppHost` | `ConsoleHost` | `WebHost` |
|---|---|---|---|---|
| Definition | Messages, in-process senders | Yes | Yes | Yes |
| Definition | Presenters, `PresenterScope`, `LiveScopes` | Yes | Yes | Yes |
| Transport | Command line: one request | No | Yes | No |
| Transport | HTTP API: one route for each request | No | No | Yes |
| Transport | Views (`ViewsRoot`): Razor components | No | No | Yes |

```
Presenter → IRequestSender.SendAsync(request) → RequestHandler<,>             every host
API client → HTTP /api/{group}/{name} → Operation → RequestHandler<,>          web host
Command line → {group} {name} --arg value → Operation → RequestHandler<,>      console host
```

A request is the message that comes from outside the app. A command is the message between features, so a host does not expose commands. The VSA kernel makes one `Operation` for each request handler. `Invoke` sends the request with `IRequestSender`. The hosts read only the `Operation` list, so they know no VSA type.

```csharp
public interface IRequest : IMessage;

public sealed record ListNotesRequest : IRequest<ListNotesResponse> {
    public static Effect Effect => Effect.Read;
}
```

| Member | Default | Web | Console |
|---|---|---|---|
| `Group` | The module name, kebab case | Route segment | First word, optional |
| `Name` | The type name with no `Request` suffix, kebab case | Route segment | Second word, optional |
| `Effect` | `Write` | `Read` GET, `Write` POST, `Delete` DELETE | Not used |

A request declares `Group`, `Name` and `Effect` as public static properties, by convention. `RequestOperations` reads them by reflection, so the same request works on each target. `Umpire.dll` has one build, `netstandard2.1`, so `IRequest` has no `static virtual` members.

- `Effect` is in `Umpire.Messages.Contracts` (#199).
- `RequestOperations` reads the metadata with no generic call, and it makes the generic call only when a host invokes the operation. A host with no transport (for example Unity) never invokes one, so IL2CPP needs no generic instance at compose.
- A response that implements `IExitCode` gives the exit code of the console. An error gives 1. Else the code is 0.
- If the app has one request, the console words are optional. Else the console needs the name, or the group and the name.
- `IResponse` is the marker of a response type. `IRequest<TResponse>`, `RequestHandler<,>` and `IRequestSender` require it. Umpire.Sdk also uses it to find the contract files of a slice.

## AppTypes finds the parts of the app

`AppTypes` (Umpire.Kernels) reads 2 sources and keeps each loaded assembly that references a given assembly:

| Source | Finds |
|---|---|
| The loaded assemblies | Unity: all script assemblies load when the domain starts |
| The `.dll` files in `AppContext.BaseDirectory` | .NET: a module assembly loads only when code uses one of its types |

- The scan reads the metadata of a file with `System.Reflection.Metadata`. It loads only the files that reference the assembly.
- A file that the runtime cannot load by name (not in `deps.json`) is not part of the app. The scan ignores it.
- Each source finds nothing where it does not apply, so the scan has no Unity case.
- If an assembly has types that do not load, the scan keeps the types that load (`ReflectionTypeLoadException`).
- The scan skips each assembly that references `nunit.framework`: a test assembly is not part of the app.
- To select other modules, give a module provider in `AppHost.ModuleProvider` (`IModuleProvider`). The default is `ScannedModules` (the scan). `FixedModules(params Module[])` is a list by hand. It refuses a module type or a module name that it has two times. A custom provider calls `scan.In(assemblies)` to scan only some assemblies, for example a host that runs several apps in one process. `scan.In` does not skip an assembly that references `nunit.framework`: the provider selects its assemblies itself.
- A provider gets only the module scan (`ModuleScan`), not the `AppTypes` instance. Reason: the type scans of `AppTypes` read the assemblies of the modules, so a provider that read them would ask for its own modules again.
- `Build` makes one `AppTypes` instance: `new AppTypes(ModuleProvider, app.GetType().Assembly)`. The instance keeps the types of each assembly and the assemblies that reference each assembly, so it reads each assembly one time. The assemblies of the modules are also the scope of the other type scans (handlers, views, `[AutoRegister]`). A second `Start` makes a new instance, so it also finds the assemblies that loaded later.
- `LoadedTypes.Of(assembly)` (Umpire.Architecture) is the one load-safe read of the types of an assembly. The kernel, the unit test base and the rule engine use it.

| Member | Gives |
|---|---|
| `new AppTypes(provider, appAssembly)` | The one constructor. With no provider, it uses `ScannedModules`. `AppHost` gives the assembly of the app class (the root project) |
| `Modules` | The modules of the provider, in order |
| `IModuleProvider.Modules(scan)` | The modules of the app. `ScannedModules` gives `scan.All()`, `FixedModules` gives its list |
| `ModuleScan.All()`, `ModuleScan.In(assemblies)` | One module for each module class of the scan, or of the given assemblies |
| `ConcreteTypesOf(baseType)` | The concrete classes that derive from `baseType`, in the assemblies of the modules and the assembly of the app class. A type in another assembly, for example a test assembly, is not found |
| `Registered()`, `Implementations(baseType)` | The marked types (see "One discovery path") |

| Part | Scan for | Rule |
|---|---|---|
| Module root | A reference to `Umpire.Kernels`, or to an assembly with an abstract `Module` base (for example `Plugin` in `Umpire.Vsa.Composition`) | One class that inherits `Module` in each namespace (`ModuleRoot.Create`) |
| Views root | The assemblies of the modules and of the app class (`ConcreteTypesOf(typeof(ViewsRoot))`) | One component that inherits `ViewsRoot` |

The modules are in order of `Module.Order`, then by name: a plugin (`Order` 0) comes before a module (`Order` 1). If the app has a views root, the views run in that process. Two views roots stop the build.

## One discovery path

`AppTypes` finds the types of the modules one time. Each layer registration reads the same result. It reads the root assembly and each layer assembly of a module, so a module that is one assembly (for example one Unity `.asmdef`) also works. Each send goes through a pipeline (`docs/handler-pipeline.md`).

```text
AppTypes.Registered()
├─ each concrete class with [AutoRegister]     closed class: as it is
│  (on the class, a base class or an interface) open class: closed for each [AutoClose] type that fits
└─ the core step                               registers each type and each marked base
   The layer registrations                     add only the sender, the pipeline and the checks
```

| Marker | Side | Meaning |
|---|---|---|
| `[AutoRegister(lifetime)]` | Open or closed class, its base class or its interface | VSA registers each concrete class. Lifetime default: `Scoped`. |
| `[AutoClose]` | Type argument, its base class or its interface | Closes open generic types over this type. A struct or a concrete class. |
| `[assembly: AutoClose(typeof(int))]` | Type argument | A type that cannot have the attribute. |

The markers are in `Umpire.Markers`. `MarkedTypes` reads them by full name. They are the only markers of the core, because the scan needs them on each host.

The template bases have `[AutoRegister]`: `CommandHandler<,>`, `RequestHandler<,>`, `IntegrationEventHandler<>`, `Validator<>`, the four hook bases and `Presenter`. A class that derives from one of them needs no marker.

```csharp
[AutoClose]
public interface IResource;

public readonly record struct Health(int Value) : IResource;

sealed class ChangeResourceHandler<T> : CommandHandler<ChangeResourceCommand<T>, int>
    where T : struct, IResource { ... }                     // closed for Health, Mana

[AutoRegister]
public interface IDamageRule;
sealed class DamageRule<T> : IDamageRule where T : struct, IWeapon { ... }   // closed for Sword, Dagger

[AutoRegister(Lifetime.Singleton)]
sealed class WeaponCache<T> where T : struct, IWeapon { ... }
```

### Rules

| Rule | Detail |
|---|---|
| Registration | The class itself, and each base class or interface with the marker. |
| One instance | Each marked base resolves the class itself, so a scope has one instance. |
| Type arguments | `[AutoClose]` types, assembly types and the constructor walk. |
| Constraints | `class`, `struct`, `new()`, `unmanaged` and each type constraint. |
| Parameters | One or more. Each parameter gets each `[AutoClose]` type that fits. |
| No marker | VSA does not close or register the class. |
| No type argument fits | The start stops, with a message, except for an open handler (see below). |
| Owner | A layer registration can register a class first. The core step does not replace it. |

`AppTypes` is a singleton service. `Implementations(typeof(TBase))` gives a `ServiceImplementation(Service, Implementation)` for each registered type that derives from `TBase`, with that base as the service. A plugin reads its own types with it.

The type arguments of an open class are the marked types that meet each constraint of its generic parameters (`ConstraintTypeArguments`, internal). The check substitutes the other parameters, so `where TTo : IRate<TFrom>` works. An app cannot replace this source.

## Open generics

The container never closes a generic type itself. `CloseOpenDescriptors` replaces each open descriptor with closed descriptors before the build. Reason: Microsoft DI copies the type arguments by position, so it cannot close a nested shape such as `Entry<T> : IEntry<Put<T>>`. IL2CPP and NativeAOT also refuse a struct type argument that has no code at build time.

```text
AppHost.Compose
└─ CloseOpenDescriptors(services, types)
   ├─ open descriptors       each module descriptor with an open ServiceType and ImplementationType
   ├─ 1. [AutoClose] types   each fit closes the implementation; each matching service is added
   ├─ declared messages      each open handler is closed for each concrete message of its shape
   ├─ 2. constructor walk    each closed parameter, or the item of IEnumerable<T>, is a closed request
   │                         OpenGenerics.Unify(open implementation, request) solves the type parameters
   │                         an open [AutoRegister] class also answers a request
   │                         repeat until no new type
   ├─ remove                 an open descriptor with one or more known closed forms
   ├─ remove duplicates      one descriptor for each (service, implementation), the last one stays
   └─ stop the start         an open [AutoRegister] class with no closed type, except an open handler
```

| Item | Rule |
|---|---|
| Lifetime | Each closed descriptor has the lifetime of its open descriptor. |
| Known closed form | A closed form that a module registers also counts. |
| No closed form | The open descriptor stays, and the container closes it at use. |
| Kernel descriptors | Not changed, for example `ILogger<>` and `IOptions<>`. |
| Duplicates | A module registration of a scanned class gives one list item. |
| Dispatch | The pipeline closes an open handler, validator or hook for a sent message with the same matcher. |

`OpenGenerics.Unify(openImplementation, service)` matches the closed service against each type that the open class implements: the class, its base classes and its interfaces. It solves each type parameter, then it checks each constraint itself, because IL2CPP does not check constraints in `MakeGenericType`.

```csharp
OpenGenerics.Unify(typeof(Entry<>), typeof(IEntry<Put<Coin>>));          // Entry<Coin>
OpenGenerics.Unify(typeof(Exchange<,>), typeof(IExchange<Gem, Coin>));    // Exchange<Gem, Coin>
OpenGenerics.Unify(typeof(Counter<>), typeof(ICounter<int>));             // null: int is not IAmount
```

### Open handlers

An open handler is an open class of a dispatch service: `IHandler<>`, `Validator<>` or a hook base of a pipeline. Each pipeline definition gives its message base and these services to the kernel (`IDispatchClose`). The message shape is the first type argument of the dispatch service.

| Open handler | Message shape | Result |
|---|---|---|
| `PutHandler<T> : CommandHandler<Put<T>, int>` | `Put<T>` | Dispatch closes it. No marker |
| `SwapHandler<TA, TB> : CommandHandler<Swap<TA, TB>, string>` | `Swap<TA, TB>` | Dispatch closes it. No marker |
| `JsCommandHandler<TResult> : CommandHandler<JsCommand<TResult>, TResult>` | `JsCommand<TResult>` | Also closed at start for `MotionWait : JsCommand<WaitResult>` |
| `Bad<T, U> : CommandHandler<Put<T>, int>` | `Put<T>`, no `U` | The start stops |

- If each type parameter of the class is in the message shape, the dispatch can close it, so the start does not stop.
- At start, each concrete message class of the module assemblies that has the shape closes the handler. The closed handler is registered as the scan registers it.
- If a send finds no handler, and an open handler has the shape but a type argument does not meet a constraint, the error names the handler, the message and the constraint:

```text
Till.Slices.Pick.PickCommand`1[System.Int32] has no handler in its pipeline. Till.Slices.Pick.PickHandler<T> cannot close for PickCommand`1: Int32 does not meet the constraints of T: struct, IAmount. Change the message, or change the constraints of the open handler.
```

`MessageTypes.Closed` (the dispatch close) supports more than one type parameter: `OpenGenerics.ForMessage` finds the message shape in the message, its base classes or its interfaces, then it solves each parameter.

### Start error

Each other open `[AutoRegister]` class with no closed type stops the start, for example a service:

```text
Shop.Domain.Rack<T> has no type argument. Mark a type that meets the constraints of T with [AutoClose], or remove [AutoRegister].
```

The check applies to each open `[AutoRegister]` class of the module assemblies. These are the only assemblies that the scan reads. A library type that an app may not use has no `[AutoRegister]`, or it is outside the module assemblies. Thus `[AutoRegister]` has no opt-out (MgCohen/vsa-template#214).

The guide with the use cases: https://claude.ai/artifact/5xxwYSBS2MNS3kupBP61rT (a private page). This file is the repository source.

### Reflection uses

Umpire uses reflection only in the places below. Each other part gets its types from `AppTypes`.

| Place | Use | Reason |
|---|---|---|
| `AppAssemblies` (Kernel) | `AppDomain.GetAssemblies`, `Assembly.Load`, `PEReader` | Finds the module assemblies. Only the `AppTypes` instance calls it |
| `LoadedTypes.Of` (Architecture) | `GetTypes` | The one load-safe type read |
| `ModuleRoot.Create` (Kernel) | `Activator.CreateInstance` | Creates the module class. A class with no public constructor with no parameters stops the start, with a message |
| `OpenGenerics`, `ConstraintTypeArguments` (Kernel) | `MakeGenericType`, `GetFields` | Closes an open class: the scan, the descriptor close and the dispatch |
| `OpenDescriptors` (Kernel) | `GetConstructors`, `GetParameters` | The constructor walk and the declared message close of `CloseOpenDescriptors` |
| `MarkedTypes` (Kernel) | `GetCustomAttributesData` | Reads `[AutoRegister]` and `[AutoClose]` by name |
| `MessageRoute` (Messages.Handlers) | `MakeGenericMethod` | Makes the envelope and the typed handler call of the runtime message type |
| `Pipeline` (Messages.Handlers) | `ActivatorUtilities.CreateInstance` | Creates the runner. A runner that it cannot create stops the send, with the pipeline and runner names |
| `PipelineRun` (Messages.Handlers) | `GetServiceOrCreateInstance` | Creates the steps and hooks from their types |
| `Views` (Hosts.Web) | `MakeGenericMethod` | Calls `MapRazorComponents<TRoot>` for the views root |
| `RequestOperations` (Composition) | `MakeGenericMethod` | Reads the static `Group`, `Name` and `Effect` of each request, and makes its `Operation` |
| `CommandLine` (Hosts.Console) | `GetConstructors`, `Invoke` | Binds the command-line values to the constructor of the request |
| `VsaUnitTest` (Testing.Unit) | `LoadedTypes.Of`, `MakeGenericType` | Finds the handler of a request or a command |
| `AppLoader` (engine) | `AssemblyLoadContext`, `Activator`, `GetProperty` | Creates the one definition type: the app class (then reads its architecture) or the architecture |
| `RuleAnalyzer` (Kernels.Analyzers) | `GetTypes`, `Activator` | Finds the rule classes in its own assembly, in the compiler |

The table does not list the uses that only read metadata, for example `IsAssignableFrom`, `GetInterfaces` and `GetGenericArguments`.

### Checks

The checks read the registered types. They do not find types.

| Check | Rule |
|---|---|
| Commands | One handler for each command |
| Requests | One handler for each request |

## The HTTP API

`WebHost` maps one route for each `Operation`, with ASP.NET Core minimal APIs (`MapMethods`). The route is `Route(operation)`, by default `/api/{group}/{name}`. A web host can override `Route`.

| Step | Source |
|---|---|
| Bind the request | The JSON body (`POST`, `PUT`, `PATCH`), then the route values, then the query |
| Run the handler | `Operation.Invoke`, with the services of the scope of the HTTP request |
| Success | 200 with the response as JSON |
| Error of the handler | A problem response: 404 if the code ends with `.not_found`, else 400 |
| Value that does not bind | A problem response, 400, title `request.not_valid` |

`Invoke` sends each request with the registered `IRequestSender`. If a module replaces `IRequestSender`, for example with a remote sender, the HTTP API uses that replacement too. A remote sender that calls the same app makes a loop.

If the app has one `ViewsRoot`, `WebHost` adds `AddRazorComponents().AddInteractiveServerComponents()` and maps `MapRazorComponents<TRoot>()` (#204). `Umpire.Vsa.Views` has only the presenter components (`PresenterWatcher`, `PresenterComponent<T>`).

## Projects

```
framework/Kernel/                      Umpire.Kernels
  App/                                 App, App<TPack>, IApp, Pack, Pack<TArchitecture, TKernel, THost>, AppStop
  Kernels/                             Kernel, the core step (AutoRegistration), OpenDescriptors
  Operations/                          Operation, OperationInvoke, OperationResult
  Hosts/                               AppHost
  Hosts/Web/                           Umpire.Hosts.Web: WebHost, ViewsRoot, the HTTP API, the request binder
  Hosts/Console/                       Umpire.Hosts.Console: ConsoleHost, the command-line binder
  Modules/                             Module, Module<TSettings>, ModuleSettings, ISettingsProvider, UmpireConfigSettings
  Services/                            ServiceCollectionExtensions: Replace<TService, TImplementation>
  Scan/                                AppTypes, ModuleRoot, ServiceImplementation, ConstraintTypeArguments, MarkedTypes, OpenGenerics
  Factories/                           IFactory<T>, AddFactory
architecture/                          Umpire.Architecture: Architecture (the kernel references it for Pack)
framework/Components/Messages/
  Contracts/                           IMessage, Envelope, Result, Error, Effect
  Handlers/                            PipelineBuilder, Pipeline, Pipelines, InlineRunner, Validator, Before, After, AddPipeline
packs/Vsa/
  VsaPack.cs                           Umpire.Vsa: Vsa<THost>, Vsa<TArchitecture, THost>
  Definition/                          Umpire.Vsa.Definition: VsaArchitecture, the VSA layers, Feature and Plugin
  Kernel/
    VsaKernel.cs                       Umpire.Vsa.Composition: VsaKernel
    Handlers/Events/                   AddEventHandlers, InProcessEventPublisher
    Handlers/Commands/                 AddCommandHandlers, InProcessCommandSender
    Handlers/Requests/                 RequestHandler, AddRequestHandlers
    Presenters/                        AddPresenters
    Views/                             PresenterWatcher, PresenterComponent<T>
    Composition/                       Umpire.Vsa.Composition: RequestOperations, Plugin, Plugin<TSettings>
```

```mermaid
flowchart BT
    Kernel["Umpire.Kernels"] --> Definition["Umpire.Architecture"]
    Kernel --> Contracts["Umpire.Messages.Contracts"]
    Web["Umpire.Hosts.Web"] --> Kernel
    Console["Umpire.Hosts.Console"] --> Kernel
    Composition["Umpire.Vsa.Composition"] --> Kernel
    Composition --> Layers["Umpire.Vsa layers"]
    VsaDefinition["Umpire.Vsa"] --> Composition
```

- The kernel knows no layer. `VsaKernel.Configure` calls each layer registration by a direct call, in a fixed order.
- No host knows VSA. The hosts read only `Operation`.
- `Umpire.Kernels` does not reference `Microsoft.Extensions.Hosting`. Only `Umpire.Hosts.Web` uses a .NET Generic Host, inside `WebApplication`.
- `Umpire.Vsa` references `Umpire.Vsa.Composition`, because its definition counts the `Plugin` classes with `HasTypes`, and `Vsa<THost>` names `VsaKernel`. `Umpire.Vsa.Composition` does not reference `Umpire.Vsa`.

## The container

Module code and the layer registrations use two shared interfaces from .NET:

| Interface | Job | Package |
|---|---|---|
| `IServiceCollection` | Registration: a list of `ServiceDescriptor` | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `IServiceProvider` | Resolution: `GetService(Type)` | `System` |

`AppHost.Build` builds the Microsoft container. An app with its own container calls `Compose`, then builds its container. Other containers read `IServiceCollection` too:

| Container | Reads `IServiceCollection` |
|---|---|
| Autofac | `builder.Populate(services)` (`Autofac.Extensions.DependencyInjection`) |
| DryIoc, Lamar, SimpleInjector | Their own integration packages |
| VContainer | No support. The app routes to the Microsoft container |

An app with a container that cannot read `IServiceCollection` builds the Microsoft container from `Compose`, then routes to it: for each service type, its container gets a registration that resolves the service from the Microsoft container. So the services keep the Microsoft semantics. A copy of each `ServiceDescriptor` often does not work, because the app registers several instances of one type and reads them as a list (`AddPipeline`).

## Factories

A factory makes objects that the container cannot make, for example Unity objects from a prefab. The factory gets one service, and it gives the made object what it needs. Only the composition root and the factories resolve services. The made objects do not get `IServiceProvider`.

| Type | Use |
|---|---|
| `IFactory<T>` | `T Create()` |
| `services.AddFactory<T, TService>(create)` | A factory: a function of one service |

```csharp
services.AddFactory<CounterLabel, CounterPresenter>(presenter => {
    var label = Object.Instantiate(labelPrefab);
    label.Bind(presenter);
    return label;
});
```

A factory resolves its service on each `Create`, from the scope that resolved the factory. A factory knows no view and no presenter: views, enemies and other objects use the same factories. For a factory with more services, write a class that implements `IFactory<T>` and register it as a normal service.

## Module settings

```text
module.Install(services, settings)                 public, from AppHost.Compose
└─ settings.Use<TSettings>(services, this, Validate)
   ├─ providers[0].Get<TSettings>(module) → null
   ├─ providers[1].Get<TSettings>(module) → TSettings   the first answer wins
   ├─ no answer → new TSettings()
   ├─ Validate errors → InvalidOperationException, all errors in one message
   └─ services.AddSingleton(settings)
→ Configure(services, settings)
```

The host gives the providers in `AppHost.Settings(types)`. The default is `[new UmpireConfigSettings(UmpireConfig.In(AppContext.BaseDirectory))]`: the table with the module name in the `.umpireconfig` file of the output folder, for example `[Notebook]`. `In` reads only that folder, not the folders above it. The name of a module is the last part of its namespace. The SDK copies `.umpireconfig` to the output folder. With no file or no base folder, `In` gives `UmpireConfig.Empty`, and the next provider answers. The table name and the property names match with no case. A value of the wrong type stops the start with the table name, the file and the Tomlyn message. A host adds a provider in front: `[new MyProvider(), .. base.Settings(types)]`. `ModuleSettings.Get<TSettings>(module)` reads the settings with no registration.

```toml
[Notebook]
MaxNotes = 50
Limits.MaxLines = 80
Modes = { easy = 1, hard = 5 }
```

`UmpireConfigSettings` reads with Tomlyn reflection. For no reflection, give a Tomlyn context (the context sets its own options, so its property names match with case): `new UmpireConfigSettings(config, MyToml.Default)` with `[TomlSerializable(typeof(NotebookSettings))] partial class MyToml : TomlSerializerContext`. A direct call of `Configure(services)` on a settings module does nothing. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What each host gives

| Service | `WebHost` | `ConsoleHost` | `AppHost` |
|---|---|---|---|
| `ICommandSender`, `IRequestSender`, `IIntegrationEventPublisher` | In process | In process | In process |
| `ILogger<T>` | Yes, ASP.NET Core providers | Yes, no providers | Yes, no providers. A host adds them in `Configure` |
| `IConfiguration` | `appsettings.json`, environment, `args` | `args` from the first option (`--key value`) | `args` (`--key value`, `--Section:Key=Value`) |
| `IHostedService` start | Yes, inside `WebApplication` | No | No |

## Portable target

`Umpire.Kernels`, `Umpire.Vsa.Composition`, `Umpire.Architecture`, `Umpire.Vsa` and all definition layers are portable layers (`UmpirePortableLayer`): they build for `netstandard2.1` only (one build, #301). The transports (Umpire.Hosts.Web, Umpire.Vsa.Views) need ASP.NET Core, so they build for `net10.0` only. The limits of the `netstandard2.1` hosts (for example Unity) and their tests are in `docs/unity.md`.

A host package (for example vsa-unity) gives its own host, which derives `AppHost` and overrides `Configure`, for example to add a log provider of its platform. Its launcher calls `app.Start(args)` and `app.Stop()`. `Start` keeps the thread of its caller as the app thread, so `AppStop.Request` from any thread stops the app on that thread:

```csharp
public sealed class GameHost : AppHost {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.AddLogging(logging => logging.AddProvider(new GameLogProvider()));
    }
}
```

An app that owns its container calls `Compose` and builds the container itself:

```csharp
var host = new GameHost();
host.Compose(collection, configuration, new VsaKernel(), new AppTypes(appAssembly: typeof(GameApp).Assembly));
```

A bridge to another container (fallback or side by side) reads `App.Services` after `Start`.

## Non-goals

- No VSA container interface. Reason: `IServiceCollection` and `IServiceProvider` are already container-neutral, and most containers read them.
- No kernel scan. Reason: the pack names its kernel, so a kernel of another architecture in the bin folder does nothing.
- No `IHost` in Umpire.Kernels. Reason: only the web platform needs it, and Unity must not vendor the Hosting DLLs.
- No install point on `App`. Reason: the app class is the lifecycle. The custom host `Configure` is the central root install.
- No operation for a command. Reason: a command is the message between features. A request is the message from outside.
- No VContainer bridge in the framework. Reason: only a Unity app with VContainer needs it, so the app keeps it. Use the router, not a copy of the registrations.
- No Unity code in this repository (`UnityHost`, `UnityLogProvider`, a view base). Reason: it needs `UnityEngine`. The UPM package (repo `vsa-unity`) keeps it.
- No view injector and no scene scan. Reason: a factory gives each object its services, and a Unity app often has its own container for scene objects.
- No middleware hook on the web host. Reason: no app needs one. Add it when an app needs authentication or CORS before the routes.

## Open questions

- Remote request sender: an `IRequestSender` over HTTP, for a view or a Unity client in another process. It can use the same routes as the HTTP API.
- Other type sources: a source generator or a hand list, behind one source interface that an app can replace (#138). Umpire.Sdk knows the module projects at build time, so it can write the list in the host assembly.
- The pack: keep it as a join of architecture, kernel and host, or replace it at the app level (#198).
- The copy of the base `Build` in `ConsoleHost` (#203).
