# Composition: the composer and its installers

A `VsaComposer` registers an app in two calls: `InstallLayers`, then `ConfigureModules`. Each layer has one installer class in its own project. `builder.AddVsa()` (VSA.Host) calls the composer and adds the web parts between the two calls. A Unity app or a console app calls the composer from `VSA.Composition`, which has no ASP.NET Core.

## Two call points

```mermaid
flowchart LR
    D[ModuleDiscovery or given modules] --> X[VsaContext]
    X --> L[InstallLayers]
    L --> W[App code, for example the web parts]
    W --> M[ConfigureModules]
```

| Call | Runs | Registers |
|---|---|---|
| `InstallLayers()` | `EventHandlerInstaller` | The publisher and each `IntegrationEventHandler<T>` |
| `InstallLayers()` | `CommandHandlerInstaller` | The sender and each `CommandHandler<,>`. Stops at two handlers |
| `InstallLayers()` | `PresenterInstaller` (`net10.0`) | `LivePresenters` and each `Presenter` |
| `ConfigureModules()` | `ModuleInstaller` | `Module.Configure` of each module |
| `Install()` | Both calls | All of the above |

`ConfigureModules` runs last, because a module can replace a default in `Configure` (`ModuleBuilder.Replace`). App code that registers a default goes between the two calls.

## Installers find their types

An installer gets a `VsaContext`. `context.Implementations(typeof(TBase))` gives each concrete type of each module that derives from `TBase`, with its service type:

```csharp
foreach (var (service, handler) in context.Implementations(typeof(IntegrationEventHandler<>))) {
    context.Services.Add(service, handler, VsaLifetime.Scoped);
}
```

- For an open generic base, the service is the closed base, for example `IntegrationEventHandler<Rang>`.
- For a plain base, for example `Presenter`, the service is the base.
- The context reads the types of all layer projects of the modules one time. The layer rules keep each type in its layer, so no layer name is necessary.

## Projects

```
Module/
  Container/       IVsaServices, IVsaResolver, VsaLifetime
    Microsoft/     MicrosoftServices, MicrosoftResolver
  IVsaInstaller, VsaContext, ModuleInstaller, Module, ModuleBuilder, ModuleDiscovery, ...
Handlers/Events/     EventHandlerInstaller, InProcessEventPublisher
Handlers/Commands/   CommandHandlerInstaller, InProcessCommandSender
Handlers/Requests/   RequestEndpointInstaller
Presenters/          PresenterInstaller
Composition/         VsaComposer
Host/                VsaHostExtensions (AddVsa, MapVsaRequests, AddLoopbackBus), HttpBus
```

Each layer project references `VSA.Module` with `PrivateAssets="all"`. So a module project of that layer does not see `VSA.Module`. The module root references `VSA.Module`, so the app has it at run time.

## Entry points

```csharp
builder.AddVsa();

var composer = new VsaComposer(new MicrosoftServices(services), configuration, modules);
composer.Install();
```

`builder.AddVsa()` does these steps:

1. `composer.InstallLayers()`
2. `VsaHostExtensions.AddLoopbackBus(services)`: `IBus` as `HttpBus` to the server address
3. `RequestEndpointInstaller`: FastEndpoints for each `RoutedHandler<,>`
4. `composer.ConfigureModules()`

A web app with other needs does the same steps itself. With no `modules` argument, the composer finds the modules with `ModuleDiscovery`: the module roots of `deps.json`, the Shared modules first. Unity has no `deps.json`, so a Unity app gives the modules.

## The container adapter

Module code and installers register on `IVsaServices`, a VSA interface. They do not use a container type. The adapter types are in `Module/Container/`.

```csharp
public interface IVsaServices {
    void Add(Type service, Type implementation, VsaLifetime lifetime);
    void Add(Type service, Func<IVsaResolver, object> factory, VsaLifetime lifetime);
    void AddInstance(Type service, object instance);
    VsaLifetime? LifetimeOf(Type service);
    void RemoveAll(Type service);
}
```

- `IVsaServices` also has `AddSingleton`, `AddScoped` and `AddTransient`, with the same forms as Microsoft DI.
- `IVsaResolver` resolves services at run time: `Resolve`, `ResolveAll` and `Create`. The in-process publisher and sender use it.
- `MicrosoftServices` is the adapter for Microsoft DI. It also registers `IVsaResolver`.
- Another container, for example VContainer, needs one class that implements `IVsaServices` and one that implements `IVsaResolver`.

The web parts use `IServiceCollection` directly, because FastEndpoints and `AddHttpClient` need it. The web host always uses Microsoft DI.

## Module settings

`Module<TSettings>` binds `Modules:{Name}` to `TSettings` and checks it. Then it registers the instance as a singleton. Settings have no `IOptions<TSettings>` and no reload, so each container gets the same behavior.

## What a headless app gives

| Service | Web host | Headless app |
|---|---|---|
| `IBus` | `AddLoopbackBus` | The app registers one |
| `ILogger<T>` | ASP.NET Core logging | The app registers logging |
| `IConfiguration` | `builder.Configuration` | An argument of the composer |

## Unity target

`VSA.Composition` is a Unity layer (`VsaUnityLayer`). On `netstandard2.1`, `InstallLayers` does not run `PresenterInstaller`, because `VSA.Contracts.Requests` uses `static abstract` members. Issue #44 tracks this. A Unity app also vendors `VSA.Composition.dll`.

## Open questions

- In-process bus: `HttpBus` is the only `IBus`. A request handler is a FastEndpoints endpoint, so a request needs HTTP.
- Views: `AddVsaViews` is an extension of `WebApplicationBuilder`. It can become an installer of `VSA.Views`.
