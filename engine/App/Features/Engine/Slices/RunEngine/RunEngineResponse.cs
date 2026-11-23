namespace Engine.Slices.RunEngine;

using Umpire.Vsa.Contracts.Requests;

public sealed record RunEngineResponse(int ExitCode, IReadOnlyList<string> Lines) : IResponse, IExitCode;
