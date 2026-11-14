using ArchUnitNET.Domain;
using Assembly = System.Reflection.Assembly;

namespace VSA.Architecture.Engine;

public sealed class ArchitectureMap {
    readonly RuleChecks checks;

    ArchitectureMap(ArchitectureDefinition definition, LoadedApp app, Bindings bindings) {
        Definition = definition;
        App = app;
        Bindings = bindings;
        checks = new RuleChecks(definition, bindings, app.AppRoot);
    }

    public ArchitectureDefinition Definition { get; }
    public LoadedApp App { get; }
    public Bindings Bindings { get; }

    public static ArchitectureMap Load(ArchitectureDefinition definition, IEnumerable<Assembly> assemblies, string appRoot) =>
        Load(definition, assemblies.Select(a => a.Location), appRoot);

    public static ArchitectureMap Load(ArchitectureDefinition definition, IEnumerable<string> dlls, string appRoot) {
        var app = ArchitectureLoader.Load(dlls, appRoot);
        return new ArchitectureMap(definition, app, Binder.Bind(definition, app));
    }

    public IReadOnlyList<RuleFinding> Findings() => [.. Bindings.AppTypes.SelectMany(checks.Of), .. App.Warnings];

    public IReadOnlyList<RuleFinding> Findings(IType type) => checks.Of(type);
}
