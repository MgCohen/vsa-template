# Spike: an operation converter in front of the central hub

`umpire ping 5` goes from the console host to the core as an `Operation`. The architecture's one converter turns it into `PingRequest`. The hub then runs the app-wide steps and the request pipeline. An app with no converter gets a warning at start, and each operation gets an error result.

```bash
cd spikes/hub
dotnet build Entry/Umpire.Entry.csproj
dotnet artifacts~/bin/Umpire.Entry/debug/umpire.dll ping 5
dotnet build Entry.Bare/Umpire.Bare.csproj
dotnet artifacts~/bin/Umpire.Bare/debug/umpire-bare.dll ping 5
```

| Part | File | Owner |
|---|---|---|
| Arguments to `Operation` | `Hosts.Console/ConsoleHost.cs` | The host |
| `Operations`: the converter, then one new scope and the hub | `Core/Operations/Operations.cs` | The core |
| The output of warnings and errors (`IAppLog`, default `ConsoleAppLog`) | `Core/App/` | The core. A module can replace it |
| The check at start: no converter is a warning, two are an error | `Core/App/App.cs` | The core |
| `Operation` to `PingRequest`, with binders built at start | `Pack/RequestConverter.cs`, `Pack/RequestBinder.cs` | The architecture (VSA) |
| How a binder makes the request (`IConstructorFactory`: `CompiledConstructors` by default, or `ReflectedConstructors`) | `Pack/` | The architecture. A module can replace it, for example for Unity IL2CPP or a source generator later |
| The hub: hub steps, then the pipeline of the message type. One singleton (`MessageHub`) and one `IHub` for each scope, so a message runs in the scope of its sender | `Core/Hub/MessageHub.cs`, `Core/Hub/ScopedHub.cs` | The core |
| The pipeline: steps and validators by type, then the handler | `Core/Pipeline/Pipeline.cs` | The core, set up by the architecture |
| A hub step for every message | `Ping/Plugins/Tracing/TraceStep.cs` | The app (a plugin module) |
| A pipeline step for every request | `Ping/Plugins/Tracing/RequestTimer.cs` | The app (a plugin module) |
| A validator and a handler for one message | `Ping/Features/Pong/Slices/Ping/` | The app (a feature module) |
| An app with no architecture | `Entry.Bare/Program.cs` | Shows the warning |
