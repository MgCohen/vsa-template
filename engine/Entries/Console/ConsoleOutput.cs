namespace UmpireEngine;

public sealed record ConsoleOutput(int ExitCode, IReadOnlyList<string> Lines, int Errors, int Warnings);
