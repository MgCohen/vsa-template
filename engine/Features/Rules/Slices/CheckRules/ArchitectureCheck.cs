namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class ArchitectureCheck {
    readonly RuleChecks checks;

    readonly IReadOnlyList<string> innerBlocks;

    ArchitectureCheck(
        ResolvedArchitecture resolved, LoadedApp app, Bindings bindings, IReadOnlyList<string> innerBlocks) {
        Resolved = resolved;
        App = app;
        Bindings = bindings;
        this.innerBlocks = innerBlocks;
        checks = new RuleChecks(resolved, bindings);
    }

    public ResolvedArchitecture Resolved { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }

    public static ArchitectureCheck Load(Architecture definition, IEnumerable<string> dlls, string blockFolder) =>
        Load(definition, dlls, blockFolder, []);

    public static ArchitectureCheck Load(
        Architecture definition, IEnumerable<string> dlls, string blockFolder, IReadOnlyList<string> innerBlocks) {
        var resolved = new ResolvedArchitecture(definition);
        var app = AppReader.Read(dlls, blockFolder, resolved);
        var bindings = resolved.Place(app.Facts, app.BlockFolder, innerBlocks);
        return new ArchitectureCheck(resolved, app, bindings, innerBlocks);
    }

    public IReadOnlyList<Finding> Findings() => [
        .. Bindings.All.Where(b => !b.External && !b.Part.IsBlockPlace).SelectMany(checks.Of),
        .. Bindings.Unmapped.Select(RuleChecks.Unmapped),
        .. Resolved.CoveredBlocks(App.BlockFolder, innerBlocks).Select(RuleChecks.Covered),
        .. Resolved.EmptyPlaces(App.BlockFolder, innerBlocks).Select(RuleChecks.Empty),
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
