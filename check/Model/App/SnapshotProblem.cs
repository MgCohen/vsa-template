namespace Umpire.CheckModel;

public sealed record SnapshotProblem(string Project, string File, string Reason, bool IsLibrary = false, bool IsState = false);
