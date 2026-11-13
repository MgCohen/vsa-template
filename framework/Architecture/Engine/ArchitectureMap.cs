using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace VSA.Architecture.Engine;

public sealed class ArchitectureMap {
    public const string AppRootKey = "VsaAppRoot";

    readonly ArchitectureDefinition definition;
    readonly Dictionary<IType, Binding> byType;

    ArchitectureMap(ArchitectureDefinition definition, ArchModel architecture, string appRoot, IReadOnlyList<Assembly> assemblies,
        IReadOnlyList<IType> types, IReadOnlyList<Binding> bindings, IReadOnlyList<UnmappedType> unmapped) {
        this.definition = definition;
        Architecture = architecture;
        AppRoot = appRoot;
        Assemblies = assemblies;
        Types = types;
        Bindings = bindings;
        Unmapped = unmapped;
        byType = bindings.ToDictionary(b => b.Type);
        FrameworkTypes = bindings.Where(b => b.External).Select(b => b.Type).ToHashSet();
        PartInstances = [.. bindings.Where(b => !b.External).GroupBy(b => new { b.Part, b.Path }).Select(MakePartInstance)];
    }

    public ArchitectureDefinition Definition => definition;
    public ArchModel Architecture { get; }
    public string AppRoot { get; }
    public IReadOnlyList<Assembly> Assemblies { get; }
    public IReadOnlyList<IType> Types { get; }
    public IReadOnlyList<Binding> Bindings { get; }
    public IReadOnlyList<UnmappedType> Unmapped { get; }
    public IReadOnlyList<PartInstance> PartInstances { get; }
    public IReadOnlySet<IType> FrameworkTypes { get; }
    public IReadOnlyList<string> Warnings { get; private set; } = [];
    public IReadOnlyList<IType> BoundTypes => [.. Bindings.Where(b => !b.External).Select(b => b.Type)];
    public IReadOnlyList<IType> AppTypes => [.. BoundTypes.Concat(Unmapped.Select(u => u.Type))];

    public static ArchitectureMap Load(System.Type definition) => AppFinder.FromTestOutput(definition);

    public static ArchitectureMap Load(System.Type definition, string folder, string? appRoot = null) => AppFinder.FromFolder(folder, definition, appRoot);

    public static ArchitectureMap Load(ArchitectureDefinition definition, IEnumerable<Assembly> assemblies, string? appRoot = null) {
        var list = assemblies.Distinct().ToList();
        var sources = SourceFiles.Read(list.Select(a => a.Location));
        var root = Path.GetFullPath(appRoot ?? FindAppRoot(sources.Values.Select(s => s.File)));
        var architecture = new ArchLoader().LoadAssemblies([.. list]).Build();
        var names = list.Select(a => a.GetName().Name).ToHashSet();
        var order = list.Select((assembly, index) => (Name: assembly.GetName().Name!, Index: index))
            .GroupBy(a => a.Name).ToDictionary(g => g.Key, g => g.Min(a => a.Index));
        var types = architecture.Types.Where(t => names.Contains(t.Assembly.Name.Split(',')[0]))
            .GroupBy(t => t.FullName)
            .Select(copies => copies.OrderBy(t => order[t.Assembly.Name.Split(',')[0]]).First())
            .ToList();
        var outside = new List<string>();
        var unmapped = new List<UnmappedType>();
        var bindings = Binder.Bind(definition, types, sources, root, outside, unmapped);
        return new ArchitectureMap(definition, architecture, root, list, types, bindings, unmapped);
    }

    internal static ArchitectureMap Load(
        ArchitectureDefinition definition, IEnumerable<Assembly> assemblies, string? appRoot, IReadOnlyList<string> warnings) {
        var map = Load(definition, assemblies, appRoot);
        map.Warnings = warnings;
        return map;
    }

    public IEnumerable<IType> In<TLayer>() where TLayer : Layer =>
        Bindings.Where(b => b.Part.SelfAndAncestors().Any(p => p.Is(typeof(TLayer)))).Select(b => b.Type);

    public IEnumerable<Exposure> ExposuresOf(IType type) => Exposures.Of(type, FrameworkTypes.Contains);

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
            if (!byType.TryGetValue(target, out var to) || (to.Part == from.Part && to.Path == from.Path)) {
                continue;
            }
            var decision = Decide.For(definition, from, to);
            if (decision.Permission != Permission.Deny) {
                continue;
            }
            var why = decision.Rule is null
                ? "no rule allows it (DefaultPermission = Deny)"
                : $"the rule \"{decision.Rule.Text}\" denies it";
            findings.Add($"{where}: error VSA2001: {type.Name} in {from.Where} uses {to.Type.Name} in {to.Where}: {why}. "
                + "Remove the use, or change the rule in Rules.");
        }
        foreach (var exposure in ExposuresOf(type).DistinctBy(e => new { e.Target, e.Member })) {
            if (!byType.TryGetValue(exposure.Target, out var to) || (to.Part == from.Part && to.Path == from.Path)) {
                continue;
            }
            var decision = Decide.For(definition, from, to);
            if (decision.Permission != Permission.Allow || decision.Rule is not { } rule || exposure.Access.IsWithin(rule.Access)) {
                continue;
            }
            findings.Add($"{where}: error VSA2003: {type.Name} in {from.Where} exposes {to.Type.Name} in {to.Where} "
                + $"in {exposure.Member} with {exposure.Access} access, but the rule \"{rule.Text}\" allows {rule.Access} at most. "
                + "Make the member less visible, or change the rule in Rules.");
        }
        return findings;
    }

    public string Location(IType type) {
        var source = byType.GetValueOrDefault(type)?.Source ?? Unmapped.FirstOrDefault(u => u.Type.Equals(type))?.Source;
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

    PartInstance MakePartInstance(IEnumerable<Binding> instance) {
        var first = instance.First();
        var targets = Bindings
            .Where(t => !(t.Part == first.Part && t.Path == first.Path))
            .GroupBy(t => new { t.Part, t.Path })
            .Select(target => new { Types = target.Select(t => t.Type), Decision = Decide.For(definition, first, target.First()) })
            .ToList();
        var denied = targets.Where(t => t.Decision.Permission == Permission.Deny).SelectMany(t => t.Types).ToList();
        var limits = targets.Where(t => t.Decision.Permission == Permission.Allow && t.Decision.Rule is { Access: not Access.Public })
            .SelectMany(t => t.Types.Select(type => new { Type = type, t.Decision.Rule!.Access }))
            .ToDictionary(t => t.Type, t => t.Access);
        var limited = instance.Where(b => !b.FollowsUser).Select(b => b.Type).ToList();
        return new PartInstance(
            first.Where, [.. instance.Select(b => b.Type)], denied, limited, first.Part.Types, limits, FrameworkTypes);
    }

    string Relative(string file) => (Path.IsPathRooted(file) ? Path.GetRelativePath(AppRoot, file) : file).Replace('\\', '/');
}
