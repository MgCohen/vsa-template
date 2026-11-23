namespace Build.Infrastructure;

public sealed record BuildOutput(int ExitCode, IReadOnlyList<string> Lines);
