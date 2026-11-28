namespace EngineCore.Tests;

using Facts.Domain.Shared.Dll;
using Model.Slices.CheckModel;
using Umpire.CheckModel;
using Finding = Operations.Domain.Shared.Finding;

sealed class CheckedApp {
    readonly Lazy<IReadOnlyList<Finding>> findings;

    public CheckedApp(StateFolder state, IReadOnlyList<DllFacts> facts, IReadOnlyList<Finding> setup) {
        State = state;
        Facts = facts;
        Model = AppModels.Load(state);
        findings = new(() => [.. StateCheck.Run(state).Findings.Select(CheckModelHandler.Converted), .. setup]);
    }

    public StateFolder State { get; }

    public IReadOnlyList<DllFacts> Facts { get; }

    public AppModel Model { get; }

    public IReadOnlyList<AppType> Types => Model.Types;

    public IReadOnlyList<AppType> Unmapped => [.. Model.Types.Where(t => t.IsUnmapped)];

    public IReadOnlyList<Finding> Findings() => findings.Value;

    public AppType Type(string fullName) => Model.Types.Single(t => t.Name.FullName == fullName);

    public AppType Named(string name) => Model.Types.Single(t => t.Name.Name == name);

    public Placement? PlaceOf(string fullName) =>
        Model.Find(new TypeName(fullName, "")) is { } type
            ? Model.PlacementOf(type)
            : Model.Types.SelectMany(t => t.References).Select(r => r.Fact.Target).FirstOrDefault(n => n.FullName == fullName) is { } used
                ? Model.TargetOf(used)?.Placement
                : null;

    public DllType Fact(string fullName) => Facts.SelectMany(f => f.Types).First(t => t.Name.FullName == fullName);
}
