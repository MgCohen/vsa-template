namespace Umpire.CheckModel;

sealed class SnapshotSet(
    Snapshot? current,
    IReadOnlyList<LoadedSnapshot> own,
    IReadOnlyList<LoadedSnapshot> others,
    IReadOnlyList<LoadedSnapshot> libraries,
    IReadOnlyList<SnapshotProblem> problems) {
    public Snapshot? Current { get; } = current;
    public IReadOnlyList<LoadedSnapshot> Own { get; } = own;
    public IReadOnlyList<LoadedSnapshot> Libraries { get; } = libraries;
    public IReadOnlyList<LoadedSnapshot> All => [.. Own, .. others, .. Libraries];
    public IReadOnlyList<LoadedSnapshot> Saved => [.. Own.Concat(others).Where(s => !ReferenceEquals(s.Snapshot, Current))];
    public IReadOnlyList<SnapshotProblem> Problems { get; } = problems;
}
