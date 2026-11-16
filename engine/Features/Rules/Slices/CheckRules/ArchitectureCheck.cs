namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Architecture;

sealed class ArchitectureCheck {
    readonly RuleChecks checks;

    ArchitectureCheck(ResolvedArchitecture resolved, LoadedApp app, Bindings bindings) {
        Definition = resolved.Architecture;
        App = app;
        Bindings = bindings;
        checks = new RuleChecks(resolved, bindings);
    }

    public Architecture Definition { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }

    public static ArchitectureCheck Load(Architecture definition, IEnumerable<string> dlls, string appRoot) {
        var resolved = new ResolvedArchitecture(definition);
        var app = AppReader.Read(dlls, appRoot, resolved);
        return new ArchitectureCheck(resolved, app, resolved.Place(app.Facts, app.AppRoot));
    }

    public IReadOnlyList<Finding> Findings() => [
        .. Bindings.All.Where(b => !b.External).SelectMany(checks.Of),
        .. Bindings.Unmapped.Select(RuleChecks.Unmapped),
        .. checks.Counts(),
        .. checks.Matches(),
        .. App.Warnings];

    public IReadOnlyList<Finding> Findings(string fullName) {
        if (Bindings.Of(fullName) is { External: false } bound) {
            return checks.Of(bound);
        }
        return [.. Bindings.Unmapped.Where(u => u.Type.FullName == fullName).Select(RuleChecks.Unmapped)];
    }
}
