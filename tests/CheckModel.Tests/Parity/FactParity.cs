namespace CheckModel.Tests;

using Umpire.Architecture;
using Data = Umpire.CheckModel;

sealed class FactParity(EngineRun engine, Data.AppModel app) {
    static readonly string[] onlyInIntermediateLanguage = [
        "Notes.Features.Notebook.Slices.CreateNote.CreateNoteHandler -> Umpire.Messages.Contracts.Unit",
        "Notes.Features.Notebook.Slices.DeleteNote.DeleteNoteHandler -> Umpire.Messages.Contracts.Unit",
        "ShellApp.Host.Launcher`1+LauncherApp -> Umpire.Kernels.Pack",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Access",
        "Umpire.Architecture.PlaceLimit -> Umpire.Architecture.Boundary",
        "VsaApp.Host.FixedHost -> Umpire.Kernels.Module",
    ];

    readonly Bindings bindings = engine.Check.Bindings;

    public IReadOnlyList<string> Differences() => [.. bindings.All.Where(b => !b.External).SelectMany(Differences)];

    static string Traits(TypeFacts type) =>
        $"{type.IsAbstract}{type.IsStatic}{type.IsSealed}{type.IsRecord}{type.IsStruct}{type.IsInterface}{type.IsEnum}{type.IsDelegate}";

    static string Traits(Data.TypeContent type) {
        var isClass = type.Kind == Data.TypeKind.Class;
        return $"{type.IsAbstract}{type.IsStatic}{type.IsSealed && isClass}{type.IsRecord && isClass}{type.Kind == Data.TypeKind.Struct}"
            + $"{type.Kind == Data.TypeKind.Interface}{type.Kind == Data.TypeKind.Enum}{type.Kind == Data.TypeKind.Delegate}";
    }

    IEnumerable<string> Differences(Binding binding) {
        var name = binding.Type.FullName;
        if (app.Types.FirstOrDefault(t => t.Name.FullName == name) is not { } type || app.PlacementOf(type) is not { } placement) {
            yield return $"{name} has no place";
            yield break;
        }
        if (placement.Where != binding.Where || placement.FollowsUser != binding.FollowsUser) {
            yield return $"{name} is in {placement.Where}, not in {binding.Where}";
        }
        if (Traits(type.Content) != Traits(binding.Type) || type.IsNested != binding.Type.IsNested) {
            yield return $"{name} has other traits";
        }
        if (!Bound(binding.Type.BaseArguments).SetEquals(type.Content.BaseArguments.Where(IsBound).Select(a => a.FullName))) {
            yield return $"{name} has other base arguments";
        }
        var uses = type.References.Select(r => r.Fact.Target).Concat(Data.ReferenceCheck.Implied(type.Content)).Select(u => u.FullName);
        foreach (var use in Bound(binding.Type.Uses).Except(uses).Where(u => u != name)) {
            if (!onlyInIntermediateLanguage.Contains($"{name} -> {use}")) {
                yield return $"{name} has no use of {use}";
            }
        }
        var exposures = Data.Exposure.Of(app, type).Select(e => $"{e.Target.FullName} in {e.Member.Replace('+', '/')} ({e.Access})");
        foreach (var exposure in binding.Type.Exposures.Where(e => bindings.Of(e.Target) is not null)
            .Select(e => $"{e.Target} in {e.Member} ({e.Access})").Except(exposures)) {
            yield return $"{name} has no exposure of {exposure}";
        }
    }

    HashSet<string> Bound(IEnumerable<string> names) => [.. names.Where(n => bindings.Of(n) is not null)];

    bool IsBound(Data.TypeName name) => bindings.Of(name.FullName) is not null;
}
