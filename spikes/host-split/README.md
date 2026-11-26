# Spike: one app, many transports

This spike is for reading. Do not merge it. It shows the split of the design talk of 2026-11-26: the app has the lifecycle only, the host is the transport, and one fixed composition cycle connects them. The same app runs on two transports, and no entry needs its own app class.

The code is a small copy of the framework shapes. It does not change the framework. It has no settings, no configuration, no web host, no open generics and no real thread model.

## Build and run

```bash
cd spikes/host-split
dotnet build HostSplit.slnx
dotnet artifacts~/bin/Greeter.Console/debug/Greeter.Console.dll greet --Name Linus
dotnet artifacts~/bin/Greeter.Library/debug/Greeter.Library.dll
```

The output is `Hello, Linus.`, then `Hello, Ada.` and `Hello, Grace.`.

## Read in this order

| Step | File | What it shows |
|---|---|---|
| 1 | `Greeter/GreeterApp.cs` | The app is one line: `GreeterApp : App<PackKernel>`. It names its kernel, not a host |
| 2 | `Entries/Console/Program.cs` | The command line entry: `new GreeterApp().Run(new ConsoleHost(), args)` |
| 3 | `Entries/Library/Program.cs` | The library entry: `app.Start(new LocalHost())`, then `Send`. The second start uses `FixedModules` with the same app |
| 4 | `Runtime/App/App.cs` | The lifecycle: `Start(host)`, `Run(host)`, `Stop()`. `Start` returns the host with its own type |
| 5 | `Runtime/Hosts/AppHost.cs` | `Compose` is fixed and not virtual: core, host `Install`, kernel, modules. The host gives the platform facts (`ModuleProvider`) |
| 6 | `Hosts.Console/ConsoleHost.cs` | A transport that installs itself (`Install` adds `CommandLine`) and runs one operation |
| 7 | `Runtime/Hosts/LocalHost.cs` | A transport of local calls: `Invoke(object)` gives an `OperationResult` |
| 8 | `Pack/LocalSend.cs` | The pack adds the typed `Send<TResponse>`, so the core knows no request type |

## Projects

| Project | Target | Stands for |
|---|---|---|
| `Runtime/` | `netstandard2.0` | The core: `App`, `AppHost`, `LocalHost`, modules, operations |
| `Pack/` | `netstandard2.0` | The VSA pack: `IRequest<TResponse>`, `RequestHandler`, `PackKernel`, the typed `Send` |
| `Hosts.Console/` | `netstandard2.0` | The console host package |
| `Greeter/` | `net10.0` | The app: one feature module with one slice |
| `Entries/Console/`, `Entries/Library/` | `net10.0` | Two entries of the same app |

## Findings

1. **One app, two transports, no extra class.** Each entry is one line that gives a host. The module scan uses the assembly of the app class, so an entry in another assembly finds the modules.
2. **An instance method hides an extension method.** When `LocalHost.Send(object)` exists, C# selects it before the typed extension `Send<TResponse>`. The core method is now `Invoke`, and the pack owns the name `Send`.
3. **CA2000 at each entry.** `new ConsoleHost()` goes to `Run`, and the app disposes it at `Stop`. The analyzer cannot see this transfer, so each entry gets CA2000. The spike turns it off. Another solution is `Run<THost>()` with `new()`, but then a host cannot get values before the start, for example `ModuleProvider`.
4. **A host is for one start only.** A second `Build` on the same host throws. Each start gets a new host, as before.
5. **The console host needs the arguments in `Install`.** It keeps them in its `Build` override. We can also give the arguments to `Install`, or make them part of `Compose`.
6. **A test changes the platform facts, not the app.** `new LocalHost { ModuleProvider = new FixedModules(...) }` works with the app that ships. A root service of the app is a module (`GreetingModule`).
7. **`netstandard2.0` works for the core, the pack and the console host**, with `LangVersion latest` and the `IsExternalInit` polyfill.
8. **`LocalHost.Invoke` blocks on a task.** This is safe when the handlers use `ConfigureAwait(false)`, as `RequestOperations` does. A handler that does not use it can deadlock on a thread with a `SynchronizationContext`.
