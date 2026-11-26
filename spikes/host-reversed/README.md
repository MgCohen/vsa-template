# Spike: the reversed model

The app owns the composition. A host only opens a channel. `app.Run(params AppHost[] hosts)` is the one start method.

| Part | Folder | Owns |
|---|---|---|
| `App` | `Runtime/App/` | The lifecycle. It makes the container with `Composition` and opens each host |
| `Composition` | `Runtime/Composition/` | The fixed install order: core, kernel, modules |
| `IOperations` | `Runtime/Operations/` | The one operation flow: find by request type, scope, handler, errors |
| `AppHost` | `Runtime/Hosts/` | Communication only: `Open(IOperations, AppStop)` |
| `LocalHost`, `ConsoleHost` | `Hosts.Local/`, `Hosts.Console/` | Two transports |
| Kernel of the pack | `Pack/` | The handlers and one `Operation` for each request |
| Sample app | `Greeter/` | One module and one slice |
| Scenarios | `Scenarios/` | One worked example in each folder, with its report |
