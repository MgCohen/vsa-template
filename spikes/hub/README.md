# Spike: one input through the central hub

One console input goes through the core and the hub to a VSA-like pipeline.

```bash
cd spikes/hub
dotnet build Entry/Greeter.Entry.csproj
dotnet artifacts~/bin/Greeter.Entry/debug/Greeter.Entry.dll greet --name Ada
```

| Part | File | Owner |
|---|---|---|
| Input to `Operation` | `Hosts.Console/ConsoleHost.cs` | The host |
| `Operation` to `GreetRequest` | `Pack/RequestReader.cs`, `Pack/RequestBinder.cs` | The architecture (built at start) |
| `Operations`: readers, then the hub | `Core/Operations/Operations.cs` | The core |
| The hub: hub steps, then the pipeline of the message | `Core/Hub/Hub.cs` | The core |
| The pipeline: steps and validators by type, then the handler | `Core/Pipeline/Pipeline.cs` | The core, set up by the architecture |
| A hub step for every message | `Greeter/Plugins/Tracing/TraceStep.cs` | The app (a plugin module) |
| A pipeline step for every request | `Greeter/Plugins/Tracing/RequestTimer.cs` | The app (a plugin module) |
| A validator and a handler for one message | `Greeter/Features/Greeting/Slices/Greet/` | The app (a feature module) |
