namespace CheckModel.Tests;

using Umpire.Architecture;
using Data = Umpire.CheckModel;

sealed class FactParity(EngineRun engine, Data.AppModel app) {
    const string Program = "Program";
    const string Deconstruct = "Deconstruct(";

    static readonly string[] onlyInIntermediateLanguage = [
        "Notes.Features.Notebook.Slices.CreateNote.CreateNoteHandler -> Umpire.Messages.Contracts.Unit",
        "Notes.Features.Notebook.Slices.DeleteNote.DeleteNoteHandler -> Umpire.Messages.Contracts.Unit",
        "ShellApp.Host.Launcher`1+LauncherApp -> Umpire.Kernels.Kernel",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Access",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Boundary",
        "VsaApp.Host.FixedHost -> Umpire.Kernels.Module",
    ];

    static readonly string[] constantsOfLibraries = [
        "Rules.Slices.CheckRules.WideExposureRule -> Umpire.Architecture.Permission",
        "Blocks.Infrastructure.BlockProjects -> Umpire.Config.UmpireConfig",
    ];

    static readonly Data.ReferenceKind[] onlyInSource =
        [Data.ReferenceKind.NameOf, Data.ReferenceKind.TypeArgument, Data.ReferenceKind.Inferred];

    readonly Bindings bindings = engine.Check.Bindings;

    public IReadOnlyList<string> Differences() => [
        .. bindings.All.Where(b => !b.External).SelectMany(Differences),
        .. bindings.Unmapped.Where(u => Find(u.Type.FullName) is not { IsUnmapped: true }).Select(u => $"{u.Type.FullName} is mapped"),
        .. app.Types.Where(IsExtra).Select(t => $"{t.Name} is only in the model"),
        .. Targets().SelectMany(External)];

    static string Traits(TypeFacts type) =>
        $"{type.IsAbstract}{type.IsStatic}{type.IsSealed}{type.IsRecord}{type.IsStruct}{type.IsInterface}{type.IsEnum}{type.IsDelegate}";

    static string Traits(Data.TypeContent type) {
        var isClass = type.Kind == Data.TypeKind.Class;
        return $"{type.IsAbstract}{type.IsStatic}{type.IsSealed && isClass}{type.IsRecord}{type.Kind == Data.TypeKind.Struct}"
            + $"{type.Kind == Data.TypeKind.Interface}{type.Kind == Data.TypeKind.Enum}{type.Kind == Data.TypeKind.Delegate}";
    }

    static string Text(Data.Exposure exposure) =>
        $"{exposure.Target.FullName} in {exposure.Member.Replace('+', '/')} ({exposure.Access})";

    Data.AppType? Find(string name) => app.Types.FirstOrDefault(t => t.Name.FullName == name);

    bool IsExtra(Data.AppType type) => bindings.Of(type.Name.FullName) is null && type.Name.FullName != Program
        && bindings.Unmapped.All(u => u.Type.FullName != type.Name.FullName)
        && !(type.Content.Members.Count == 0 && app.Types.Any(t => t.Name.FullName.StartsWith(type.Name.FullName + "+", StringComparison.Ordinal)));

    IEnumerable<Data.TypeName> Targets() => app.Types.SelectMany(t => t.References.Select(r => r.Fact.Target)).Distinct();

    IEnumerable<string> External(Data.TypeName name) {
        var expected = bindings.Of(name.FullName);
        var actual = app.TargetOf(name)?.Placement;
        if (expected?.Where != actual?.Where && (expected is not null || IsLoaded(name)) && name.FullName != Program
            && !(expected is null && Find(name.FullName) is { } type && IsExtra(type) is false)) {
            yield return $"{name} is in {actual?.Where}, not in {expected?.Where}";
        }
    }

    bool IsLoaded(Data.TypeName name) => engine.App.Concat(engine.Others).Any(dll => dll.Name == name.Assembly);

    IEnumerable<string> Differences(Binding binding) {
        var name = binding.Type.FullName;
        if (Find(name) is not { } type || app.PlacementOf(type) is not { } placement) {
            yield return $"{name} has no place";
            yield break;
        }
        if (placement.Where != binding.Where || placement.FollowsUser != binding.FollowsUser) {
            yield return $"{name} is in {placement.Where}, not in {binding.Where}";
        }
        if (Traits(type.Content) != Traits(binding.Type) || type.IsNested != binding.Type.IsNested) {
            yield return $"{name} has other traits";
        }
        if (!binding.Type.MutableMembers.ToHashSet().SetEquals(type.Content.MutableMembers)) {
            yield return $"{name} can change {string.Join(", ", type.Content.MutableMembers)}, not {string.Join(", ", binding.Type.MutableMembers)}";
        }
        if (!Bound(binding.Type.BaseArguments).SetEquals(type.Content.BaseArguments.Where(IsBound).Select(a => a.FullName))) {
            yield return $"{name} has other base arguments";
        }
        foreach (var difference in Uses(binding, type).Concat(Exposures(binding, type))) {
            yield return $"{name} {difference}";
        }
    }

    IEnumerable<string> Uses(Binding binding, Data.AppType type) {
        var name = binding.Type.FullName;
        var engineUses = Bound(binding.Type.Uses);
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

    IEnumerable<string> Exposures(Binding binding, Data.AppType type) {
        var model = Data.Exposure.Of(app, type).Where(e => IsBound(e.Target)).Select(Text).ToHashSet();
        var expected = binding.Type.Exposures.Where(e => IsBoundName(e.Target))
            .Select(e => $"{e.Target} in {e.Member} ({e.Access})").ToHashSet();
        return expected.Except(model).Select(e => $"has no exposure of {e}")
            .Concat(model.Except(expected).Where(e => !e.Contains(Deconstruct)).Select(e => $"has the exposure of {e}"));
    }

    HashSet<string> Bound(IEnumerable<string> names) => [.. names.Where(IsBoundName)];

    bool IsBound(Data.TypeName name) => IsBoundName(name.FullName);

    bool IsBoundName(string name) => bindings.Of(name) is not null;
}
