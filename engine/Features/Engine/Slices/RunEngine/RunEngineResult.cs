namespace Engine.Slices.RunEngine;

using Umpire.Vsa.Contracts.Commands;

public sealed record RunEngineResult(int ExitCode, IReadOnlyList<string> Lines) : IExitCode;
