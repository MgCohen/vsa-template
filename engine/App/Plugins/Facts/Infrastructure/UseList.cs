namespace Facts.Infrastructure;

using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;
using Umpire.Architecture;
using Umpire.Utility;

sealed class UseList(TypeName owner, Source primary) {
    const string CompilerName = "<";

    readonly Dictionary<Key, DllUse> found = [];

    sealed record Key(string File, TypeName Target, DllUseKind Kind, string? Member, string? TargetMember);

    public IReadOnlyList<DllUse> All => [.. found.Values];

    public void Add(IEnumerable<TypeName> targets, DllUseKind kind, int line, string? member) {
        var at = line > 0 ? line : primary.Line;
        foreach (var target in targets) {
            Add(new DllUse(target, kind, primary.File, at, at == primary.Line ? primary.Column : 1) { Member = member });
        }
    }

    public void Add(IEnumerable<BodyUse> uses, IReadOnlyList<SourcePoint> points, string? member) {
        foreach (var use in uses) {
            var point = PointOf(points, use.Offset);
            var isOwn = point is not null && !BuildOutput.Contains(point.File);
            Add(new DllUse(
                use.Target, use.Kind, isOwn ? point!.File : primary.File, isOwn ? point!.Line : primary.Line,
                isOwn ? point!.Column : primary.Column) {
                Member = member,
                TargetMember = use.TargetMember,
            });
        }
    }

    void Add(DllUse use) {
        if (use.Target == owner || use.Target.FullName.Length == 0 || use.Target.FullName.Contains(CompilerName)) {
            return;
        }
        var key = new Key(use.File, use.Target, use.Kind, use.Member, use.TargetMember);
        if (!found.ContainsKey(key)) {
            found.Add(key, use);
        }
    }

    static SourcePoint? PointOf(IReadOnlyList<SourcePoint> points, int offset) =>
        points.LastOrDefault(p => p.Offset <= offset) ?? (points.Count > 0 ? points[0] : null);
}
