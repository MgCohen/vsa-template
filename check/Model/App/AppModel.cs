namespace Umpire.CheckModel;

using System.Collections.Concurrent;

public sealed class AppModel {
    readonly ConcurrentDictionary<object, Lazy<object>> once = new();
    readonly Dictionary<string, AppType> byName = new(StringComparer.Ordinal);
    readonly Dictionary<TypeName, AppType> byType = [];
    readonly HelperPlaces helpers;
    readonly Places places;
    readonly FolderPaths paths;

    AppModel(Definition definition, BlockSettings settings, StateFolder state, CurrentProject? current, bool isDefinitionOfBlock) {
        var block = state.BlockOf(settings);
        var snapshots = new SnapshotSet(state, settings, current?.Snapshot);
        Definition = definition;
        Settings = settings;
        Block = block;
        Parts = Part.All(definition);
        Decisions = new Decisions(definition, Parts);
        Snapshots = snapshots.Own;
        Problems = snapshots.Problems;
        InnerBlocks = [.. settings.InnerBlocks.Select(folder => BlockPath.In(block, folder))];
        paths = new FolderPaths(block, [.. definition.FolderAliases, .. settings.FolderAliases], settings.IgnoredFolders);
        places = new Places(Parts, paths, InnerBlocks);
        Known = [.. snapshots.All.SelectMany(snapshot => TypesOf(snapshot, current))];
        foreach (var type in Known) {
            places.Place(type);
            byType[type.Name] = type;
            if (!byName.ContainsKey(type.Name.FullName)) {
                byName.Add(type.Name.FullName, type);
            }
        }
        helpers = new HelperPlaces(Known);
        Saved = snapshots.Saved;
        Loaded = snapshots.All;
        Types = [.. Known.Where(t => t.IsUnmapped || t.Placement is { IsExternal: false })];
        Trust = new Trust(this, snapshots, helpers, isDefinitionOfBlock);
    }

    public Definition Definition { get; }
    public BlockSettings Settings { get; }
    public string Block { get; }
    public IReadOnlyList<Part> Parts { get; }
    public Decisions Decisions { get; }
    public IReadOnlyList<LoadedSnapshot> Snapshots { get; }
    public IReadOnlyList<SnapshotProblem> Problems { get; }
    public IReadOnlyList<string> InnerBlocks { get; }
    public IReadOnlyList<AppType> Types { get; }
    public Trust Trust { get; }
    public IReadOnlyList<LoadedSnapshot> Saved { get; }
    public IReadOnlyList<LoadedSnapshot> Loaded { get; }

    IReadOnlyList<AppType> Known { get; }

    public static AppModel Load(StateFolder state, CurrentProject? current = null, CheckMode? mode = null) =>
        Load(state, state.ReadBlock(), current, mode);

    public static AppModel Load(StateFolder state, BlockSettings settings, CurrentProject? current = null, CheckMode? mode = null) {
        var text = state.ReadDefinition();
        return new AppModel(
            Definition.Read(text), settings with { Mode = mode ?? settings.Mode }, state, current, FileHash.OfText(text) == settings.DefinitionHash);
    }

    public static AppModel Load(Definition definition, StateFolder state, CurrentProject? current = null) =>
        new(definition, state.ReadBlock(), state, current, true);

    public IReadOnlyList<AppType> TypesOf(string project) => [.. Types.Where(t => t.Project == project)];

    public AppType? Find(TypeName name) =>
        byType.TryGetValue(name, out var exact) ? exact : byName.TryGetValue(name.FullName, out var named) ? named : null;

    public Placement? PlacementOf(AppType type) => helpers.Of(type);

    public Target? TargetOf(TypeName name) {
        if (Find(name) is { } type) {
            return PlacementOf(type) is { } placement ? new Target(name, placement, type) : null;
        }
        return places.Outside(name) is { } outside ? new Target(name, outside, null) : null;
    }

    public Decision DecisionFor(Placement from, Placement to) => Decisions.Of(from.Part, from.Values, to.Part, to.Values);

    internal bool IsIgnored(string file) => paths.IsIgnored(file);

    internal T Once<T>(object key, Func<T> make) where T : class =>
        (T)once.GetOrAdd(key, _ => new Lazy<object>(make)).Value;

    static IEnumerable<AppType> TypesOf(LoadedSnapshot loaded, CurrentProject? current) {
        var isCurrent = ReferenceEquals(loaded.Snapshot, current?.Snapshot);
        return loaded.Snapshot.Files
            .SelectMany(file => file.Types.Select(facts => new TypeFile(loaded.FileOf(file), facts, file.IsGenerated)))
            .GroupBy(file => file.Facts.Name)
            .Select(group => new AppType(
                loaded.Snapshot.Project, [.. group], isCurrent ? () => current!.References(group.Key) : null));
    }
}
