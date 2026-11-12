using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Architecture.Engine;

public sealed class AppArchitecture {
    readonly ArchitectureDefinition definition;
    readonly Dictionary<IType, Binding> byType;

    AppArchitecture(ArchitectureDefinition definition, ArchModel architecture, string appRoot,
        IReadOnlyList<IType> types, IReadOnlyList<Binding> bindings, IReadOnlyList<(IType Type, Source Source)> unmapped) {
        this.definition = definition;
        Architecture = architecture;
        AppRoot = appRoot;
        Types = types;
        Bindings = bindings;
        Unmapped = unmapped;
        byType = bindings.ToDictionary(b => b.Type);
        PartInstances = [.. bindings.Where(b => !b.External).GroupBy(b => (b.Part, b.Instance)).Select(MakePartInstance)];
    }

    public ArchitectureDefinition Definition => definition;
    public ArchModel Architecture { get; }
    public string AppRoot { get; }
    public IReadOnlyList<IType> Types { get; }
    public IReadOnlyList<Binding> Bindings { get; }
    public IReadOnlyList<(IType Type, Source Source)> Unmapped { get; }
    public IReadOnlyList<PartInstance> PartInstances { get; }
    public IReadOnlyList<IType> BoundTypes => [.. Bindings.Where(b => !b.External).Select(b => b.Type)];
    public IReadOnlyList<IType> AppTypes => [.. BoundTypes.Concat(Unmapped.Select(u => u.Type))];

    public static AppArchitecture Load(DiscoveredApp app) => Load(app.Definition, app.Assemblies, app.AppRoot);

    public static AppArchitecture Load(ArchitectureDefinition definition, IEnumerable<Assembly> assemblies, string? appRoot = null) {
        var list = assemblies.Distinct().ToList();
        var sources = SourceFiles.Read(list.Select(a => a.Location));
        var root = Path.GetFullPath(appRoot ?? FindAppRoot(sources.Values.Select(s => s.File)));
        var architecture = new ArchLoader().LoadAssemblies([.. list]).Build();
        var names = list.Select(a => a.GetName().Name).ToHashSet();
        var types = architecture.Types.Where(t => names.Contains(t.Assembly.Name.Split(',')[0])).ToList();
        var outside = new List<string>();
        var unmapped = new List<(IType, Source)>();
        var bindings = Binder.Bind(definition, types, sources, root, outside, unmapped);
        return new AppArchitecture(definition, architecture, root, types, bindings, unmapped);
    }

    public IEnumerable<IType> In<TLayer>() where TLayer : Layer =>
        Bindings.Where(b => b.Part.SelfAndAncestors().Any(p => p.Is(typeof(TLayer)))).Select(b => b.Type);

    public IReadOnlyList<string> Findings(IType type) {
        var where = Location(type);
        if (!byType.TryGetValue(type, out var from)) {
            return Unmapped.Any(u => u.Type.Equals(type))
                ? [$"{where}: error VSA2102: {type.Name} is in a folder that the definition does not have. Move it to a part, or add the folder in Define."]
                : [];
        }
        var findings = new List<string>();
        if (!from.FollowsUser && from.Part.Types.Count > 0 && !from.Part.Types.Any(t => type.Is(t))) {
            findings.Add($"{where}: error VSA2101: {type.Name} is in {from.Where}, but it is not "
                + $"{string.Join(" or ", from.Part.Types.Select(t => t.Name))}. Move it, or change its base type.");
        }
        foreach (var target in type.Dependencies.Select(d => d.Target).Distinct()) {
            if (!byType.TryGetValue(target, out var to) || (to.Part == from.Part && to.Instance == from.Instance)) {
                continue;
            }
            var (access, pair) = Decide.For(definition, from, to);
            if (access != Permission.Deny) {
                continue;
            }
            var why = pair is null ? "no rule allows it (Unlisted = Deny)" : $"the rule \"{pair.Rule}\" denies it";
            findings.Add($"{where}: error VSA2001: {type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
        foreach (var exposure in Exposures.Of(type).DistinctBy(e => (e.Target, e.Member))) {
            if (!byType.TryGetValue(exposure.Target, out var to) || (to.Part == from.Part && to.Instance == from.Instance)) {
                continue;
            }
            var (permission, pair) = Decide.For(definition, from, to);
            if (permission != Permission.Allow || pair is null || exposure.Access.IsWithin(pair.Access)) {
                continue;
            }
            findings.Add($"{where}: error VSA2003: {type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, but the rule \"{pair.Rule}\" allows {pair.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
        return findings;
    }

    public string Explain(IType type) {
        var findings = Findings(type);
        if (findings.Count > 0) {
            return string.Join(Environment.NewLine, findings);
        }
        return byType.TryGetValue(type, out var from) ? $"{Location(type)}: {type.Name} in {from.Where}" : $"{Location(type)}: {type.Name}";
    }

    public string Location(IType type) {
        var source = byType.GetValueOrDefault(type)?.Source ?? Unmapped.FirstOrDefault(u => u.Type.Equals(type)).Source;
        return source is null ? type.FullName : $"{Relative(source.File)}({source.Line})";
    }

    static string FindAppRoot(IEnumerable<string> files) {
        foreach (var file in files) {
            for (var folder = Path.GetDirectoryName(file); folder is not null; folder = Path.GetDirectoryName(folder)) {
                if (File.Exists(Path.Combine(folder, "global.json"))) {
                    return folder;
                }
            }
        }
        throw new InvalidOperationException("The app has no global.json above its source files. Add global.json to the app root.");
    }

    PartInstance MakePartInstance(IGrouping<(Part Part, string Instance), Binding> instance) {
        var first = instance.First();
        var targets = Bindings
            .Where(t => !(t.Part == first.Part && t.Instance == first.Instance))
            .GroupBy(t => (t.Part, t.Instance))
            .Select(target => (Types: target.Select(t => t.Type), Decision: Decide.For(definition, first, target.First())))
            .ToList();
        var denied = targets.Where(t => t.Decision.Permission == Permission.Deny).SelectMany(t => t.Types).ToList();
        var limits = targets.Where(t => t.Decision.Permission == Permission.Allow && t.Decision.Pair is { Access: not Access.Public })
            .SelectMany(t => t.Types.Select(type => (Type: type, t.Decision.Pair!.Access)))
            .ToDictionary(t => t.Type, t => t.Access);
        var limited = instance.Where(b => !b.FollowsUser).Select(b => b.Type).ToList();
        return new PartInstance(first.Where, [.. instance.Select(b => b.Type)], denied, limited, first.Part.Types, limits);
    }

    string Relative(string file) => (Path.IsPathRooted(file) ? Path.GetRelativePath(AppRoot, file) : file).Replace('\\', '/');
}
