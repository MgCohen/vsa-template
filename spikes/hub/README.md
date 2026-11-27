# Spike: operations, the central hub and AppInstance

The shape only. `umpire ping 5`: the host makes an `Operation`, the converter of the architecture makes `PingRequest`, the hub runs its steps and then the pipeline of the message.

```bash
cd spikes/hub
dotnet build Entry/Umpire.Entry.csproj
dotnet artifacts~/bin/Umpire.Entry/debug/umpire.dll ping 5
```

| File | What |
|---|---|
| `Entry/Program.cs` | The entry, one line |
| `Core/ConsoleHost.cs` | Arguments to `Operation` |
| `Core/App.cs` | `App.Run(hosts)` composes and returns `AppInstance`. The instance converts operations, sends messages and stops |
| `Core/Hub.cs` | The hub: hub steps, then the pipeline of the message type |
| `Core/Pipeline.cs` | Steps and validators by type, then the handler |
| `Core/Operation.cs` | `Operation`, `IOperationConverter`, `IRequest` |
| `Pack/` | VSA: the request handler base, the converter and the kernel |
| `Ping/` | The app: one slice, and a plugin with a hub step and a request step |
