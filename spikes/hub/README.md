# Spike: an operation on the central hub

`umpire ping 5` goes from the console host to the hub as an `Operation`. VSA subscribes a handler to the `Operation` type. That handler converts the operation to `PingRequest` and sends it back to the hub, which runs the request pipeline.

```bash
cd spikes/hub
dotnet build Entry/Umpire.Entry.csproj
dotnet artifacts~/bin/Umpire.Entry/debug/umpire.dll ping 5
```

| Part | File | Owner |
|---|---|---|
| Arguments to `Operation` | `Hosts.Console/ConsoleHost.cs` | The host |
| The hub: hub steps, then the pipeline of the message type | `Core/Hub/Hub.cs` | The core |
| The pipeline: steps and validators by type, then the handler | `Core/Pipeline/Pipeline.cs` | The core |
| A handler for `Operation`: name to request, then back to the hub | `Pack/OperationHandler.cs` | The architecture (VSA) |
| The subscriptions: the operation pipeline and the request pipeline | `Pack/PackKernel.cs` | The architecture (VSA) |
| A hub step for every message | `Ping/Plugins/Tracing/TraceStep.cs` | The app (a plugin module) |
| A pipeline step for every request | `Ping/Plugins/Tracing/RequestTimer.cs` | The app (a plugin module) |
| A validator and a handler for one message | `Ping/Features/Pong/Slices/Ping/` | The app (a feature module) |
