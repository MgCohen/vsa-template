namespace CheckModel.Tests;

using Data = Umpire.CheckModel;

sealed class FactParity(Data.AppModel dll, Data.AppModel app) {
    const string Program = "Program";
    const string Deconstruct = "Deconstruct(";

    static readonly string[] onlyInIntermediateLanguage = [
        "CodeCheck.Slices.CheckCode.CodeRun -> Operations.Domain.Shared.FindingSeverity",
        "Facts.Infrastructure.DllMemberReader -> Facts.Domain.Shared.Dll.DllAccess",
        "Model.Infrastructure.DllSnapshots -> Facts.Domain.Shared.Dll.DllMemberKind",
        "Model.Infrastructure.DllSnapshots -> Facts.Domain.Shared.Dll.DllTypeKind",
        "Model.Infrastructure.DllSnapshots -> Facts.Domain.Shared.Dll.DllUseKind",
        "Notes.Features.Notebook.Slices.CreateNote.CreateNoteHandler -> Umpire.Messages.Contracts.Unit",
        "Notes.Features.Notebook.Slices.DeleteNote.DeleteNoteHandler -> Umpire.Messages.Contracts.Unit",
        "ShellApp.Host.Launcher`1+LauncherApp -> Umpire.Kernels.Kernel",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Access",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Boundary",
        "Umpire.CheckModel.AppModel -> Umpire.CheckModel.AppReference",
        "Umpire.CheckModel.Export.DefinitionExport -> Umpire.Architecture.FilterKind",
        "VsaApp.Host.FixedHost -> Umpire.Kernels.Module",
    ];

    static readonly string[] constantsOfLibraries = [
        "Blocks.Infrastructure.BlockProjects -> Umpire.Config.UmpireConfig",
    ];

    static readonly Data.ReferenceKind[] onlyInSource =
        [Data.ReferenceKind.NameOf, Data.ReferenceKind.TypeArgument, Data.ReferenceKind.Inferred];

    public IReadOnlyList<string> Differences() => [
        .. dll.Types.Where(t => !t.IsUnmapped).SelectMany(Differences),
        .. dll.Types.Where(t => t.IsUnmapped && Find(t.Name.FullName) is not { IsUnmapped: true }).Select(t => $"{t.Name.FullName} is mapped"),
        .. app.Types.Where(IsExtra).Select(t => $"{t.Name} is only in the model"),
        .. Targets().SelectMany(External)];

    static string Traits(Data.TypeContent type) {
        var isClass = type.Kind == Data.TypeKind.Class;
        return $"{type.IsAbstract}{type.IsStatic}{type.IsSealed && isClass}{type.IsRecord}{type.Kind == Data.TypeKind.Struct}"
            + $"{type.Kind == Data.TypeKind.Interface}{type.Kind == Data.TypeKind.Enum}{type.Kind == Data.TypeKind.Delegate}";
    }

    static string Text(Data.Exposure exposure) =>
        $"{exposure.Target.FullName} in {exposure.Member.Replace('+', '/')} ({exposure.Access})";

    Data.AppType? Find(string name) => app.Types.FirstOrDefault(t => t.Name.FullName == name);

    Data.AppType? Expected(string name) => dll.Types.FirstOrDefault(t => t.Name.FullName == name);

    Data.Placement? Bound(string name) => Expected(name) is { } type ? dll.PlacementOf(type) : null;

    bool IsExtra(Data.AppType type) => Expected(type.Name.FullName) is null && type.Name.FullName != Program
        && !(type.Content.Members.Count == 0 && app.Types.Any(t => t.Name.FullName.StartsWith(type.Name.FullName + "+", StringComparison.Ordinal)));

    IEnumerable<Data.TypeName> Targets() => app.Types.SelectMany(t => t.References.Select(r => r.Fact.Target)).Distinct();

    IEnumerable<string> External(Data.TypeName name) {
        var expected = dll.TargetOf(name)?.Placement;
        var actual = app.TargetOf(name)?.Placement;
        if (expected?.Where != actual?.Where && name.FullName != Program
            && !(expected is null && Find(name.FullName) is { } type && IsExtra(type) is false)) {
            yield return $"{name} is in {actual?.Where}, not in {expected?.Where}";
        }
    }

    IEnumerable<string> Differences(Data.AppType expected) {
        var name = expected.Name.FullName;
        var where = dll.PlacementOf(expected);
        if (Find(name) is not { } type || app.PlacementOf(type) is not { } placement) {
            yield return $"{name} has no place";
            yield break;
        }
        if (placement.Where != where?.Where || placement.FollowsUser != where?.FollowsUser) {
            yield return $"{name} is in {placement.Where}, not in {where?.Where}";
        }
        if (Traits(type.Content) != Traits(expected.Content) || type.IsNested != expected.IsNested) {
            yield return $"{name} has other traits";
        }
        if (!expected.Content.MutableMembers.ToHashSet().SetEquals(type.Content.MutableMembers)) {
            yield return $"{name} can change {string.Join(", ", type.Content.MutableMembers)}, not {string.Join(", ", expected.Content.MutableMembers)}";
        }
        if (!BoundNames(expected.Content.BaseArguments).SetEquals(BoundNames(type.Content.BaseArguments))) {
            yield return $"{name} has other base arguments";
        }
        foreach (var difference in Uses(expected, type).Concat(Exposures(expected, type))) {
            yield return $"{name} {difference}";
        }
    }

    IEnumerable<string> Uses(Data.AppType expected, Data.AppType type) {
        var name = expected.Name.FullName;
        var engineUses = BoundNames(expected.References.Select(r => r.Fact.Target).Concat(Data.ReferenceCheck.Implied(expected.Content)));
        var implied = Data.ReferenceCheck.Implied(type.Content).Select(u => u.FullName).ToHashSet();
        var uses = type.References.ToLookup(r => r.Fact.Target.FullName);
        foreach (var use in engineUses.Where(u => u != name && !uses.Contains(u) && !implied.Contains(u))) {
            if (!onlyInIntermediateLanguage.Contains($"{name} -> {use}")) {
                yield return $"has no use of {use}";
            }
        }
        foreach (var use in uses.Where(u => IsBoundName(u.Key) && !engineUses.Contains(u.Key) && !u.All(IsOnlyInSource)
            && name != Program && !constantsOfLibraries.Contains($"{name} -> {u.Key}"))) {
            yield return $"has the use of {use.Key} ({string.Join(", ", use.Select(u => $"{u.Fact.Kind} {u.Fact.TargetMember}").Distinct())})";
        }
        foreach (var use in implied.Where(u => u != name && IsBoundName(u) && !engineUses.Contains(u) && !uses.Contains(u))) {
            yield return $"has the implied use of {use}";
        }
    }

    bool IsOnlyInSource(Data.AppReference use) => onlyInSource.Contains(use.Fact.Kind)
        || app.Find(use.Fact.Target) is { } target && (target.Content.Kind == Data.TypeKind.Enum || HasOnlyConstants(use, target));

    static bool HasOnlyConstants(Data.AppReference use, Data.AppType target) => use.Fact.TargetMember is { } member
        ? target.Content.Members.Any(m => m.Name == member && m.IsConst)
        : use.Fact.Kind == Data.ReferenceKind.Type && target.Content.Members.Any(m => m.IsConst);

    IEnumerable<string> Exposures(Data.AppType expected, Data.AppType type) {
        var model = Data.Exposure.Of(app, type).Where(e => IsBoundName(e.Target.FullName)).Select(Text).ToHashSet();
        var engine = Data.Exposure.Of(dll, expected).Where(e => IsBoundName(e.Target.FullName)).Select(Text).ToHashSet();
        return engine.Except(model).Select(e => $"has no exposure of {e}")
            .Concat(model.Except(engine).Where(e => !e.Contains(Deconstruct)).Select(e => $"has the exposure of {e}"));
    }

    HashSet<string> BoundNames(IEnumerable<Data.TypeName> names) => [.. names.Select(n => n.FullName).Where(IsBoundName)];

    bool IsBoundName(string name) => Bound(name) is not null || dll.TargetOf(new Data.TypeName(name, "")) is not null;
}
