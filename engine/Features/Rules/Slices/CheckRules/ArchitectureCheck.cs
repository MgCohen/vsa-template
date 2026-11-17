namespace Rules.Slices.CheckRules;

using Blocks.Domain.Shared;
using BuiltApp.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class ArchitectureCheck {
    readonly RuleChecks checks;

    readonly IReadOnlyList<string> innerFolders;

    ArchitectureCheck(ResolvedArchitecture resolved, LoadedApp app, Bindings bindings, IReadOnlyList<string> innerFolders) {
        Resolved = resolved;
        App = app;
        Bindings = bindings;
        this.innerFolders = innerFolders;
        checks = new RuleChecks(resolved, bindings);
    }

    public ResolvedArchitecture Resolved { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }

    public static ArchitectureCheck Load(
        Architecture definition, IReadOnlyList<string> dlls, IReadOnlyDictionary<string, Source> sources, BlockTree blocks) {
        var resolved = new ResolvedArchitecture(definition);
        var app = AppReader.Read(dlls, sources, blocks.Current.Folder, resolved);
        var bindings = resolved.Place(app.Facts, app.BlockFolder, blocks.InnerFolders);
        return new ArchitectureCheck(resolved, app, bindings, blocks.InnerFolders);
    }

    public IReadOnlyList<Finding> Findings() => [
        .. Bindings.All.Where(b => !b.External && !b.Part.IsBlockPlace).SelectMany(checks.Of),
        .. Bindings.Unmapped.Select(RuleChecks.Unmapped),
        .. Resolved.CoveredBlocks(App.BlockFolder, innerFolders).Select(RuleChecks.Covered),
        .. Resolved.EmptyPlaces(App.BlockFolder, innerFolders).Select(RuleChecks.Empty),
        .. checks.Counts(),
        .. checks.Matches(),
        .. App.Warnings];

    public IReadOnlyList<Finding> Findings(string fullName) {
        if (Bindings.Of(fullName) is { External: false, Part.IsBlockPlace: false } bound) {
            return checks.Of(bound);
        }
        return [.. Bindings.Unmapped.Where(u => u.Type.FullName == fullName).Select(RuleChecks.Unmapped)];
    }
}
