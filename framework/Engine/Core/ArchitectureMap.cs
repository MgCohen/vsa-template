using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed class ArchitectureMap {
    readonly RuleChecks checks;

    ArchitectureMap(ArchitectureDefinition definition, LoadedApp app, Bindings bindings) {
        Definition = definition;
        App = app;
        Bindings = bindings;
        checks = new RuleChecks(definition, bindings);
    }

    public ArchitectureDefinition Definition { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }

    public static ArchitectureMap Load(ArchitectureDefinition definition, IEnumerable<string> dlls, string appRoot) {
        var app = ArchitectureLoader.Load(dlls, appRoot);
        return new ArchitectureMap(definition, app, Binder.Bind(definition, app));
    }

    public IReadOnlyList<RuleFinding> Findings() => [
        .. Bindings.All.Where(b => !b.External).SelectMany(checks.Of),
        .. Bindings.Unmapped.Select(checks.Unmapped),
        .. App.Warnings];

    public IReadOnlyList<RuleFinding> Findings(IType type) {
        if (Bindings.Of(type) is { External: false } bound) {
            return checks.Of(bound);
        }
        return [.. Bindings.Unmapped.Where(u => u.Type.Equals(type)).Select(checks.Unmapped)];
    }
}
