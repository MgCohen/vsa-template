using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Presenters.Items;
using ArchitectureViewer.Slices.AddRule;
using ArchitectureViewer.Slices.ChangeAccess;
using ArchitectureViewer.Slices.CopyText;
using ArchitectureViewer.Slices.ExportChanges;
using ArchitectureViewer.Slices.LoadDefinition;
using ArchitectureViewer.Slices.RemoveRule;
using VSA.Contracts;

namespace ArchitectureViewer.Presenters;

public sealed partial class GraphPresenter
{
    private readonly Dictionary<GraphKind, Board> boards = [];
    private readonly List<RuleChange> ruleChanges = [];
    private Definition definition = Definition.Empty;
    private string? source;

    public GraphKind Kind { get; private set; } = GraphKind.App;

    public IReadOnlyList<string> Layers { get; private set; } = [];

    public int RuleChangeCount => ruleChanges.Count;

    public RuleDraft RuleDraft { get; private set; } = RuleDraft.Default;

    public ExportText? Export { get; private set; }

    public string? Message { get; private set; }

    public async Task ShowKind(GraphKind kind)
    {
        if (kind == Kind)
        {
            return;
        }
        boards[Kind] = new Board(data, layout, Shape, View);
        Kind = kind;
        Selection = Target.None;
        if (boards.TryGetValue(kind, out Board? board))
        {
            (data, Shape, View) = (board.Data, board.Shape, board.View);
            await Show(board.Layout);
            return;
        }
        Shape = kind.Shape;
        await Import(null);
    }

    public async Task Import(string? json)
    {
        Result<LoadDefinitionResponse> loaded = await Bus.Send(new LoadDefinitionRequest(json, Shape));
        Message = loaded.Error?.Message;
        if (!loaded.IsSuccess)
        {
            await Notify();
            return;
        }
        source = json;
        definition = loaded.Value.Definition;
        Layers = [.. definition.Parts.Where(p => p.Parent is not null)
            .Select(p => p.Layer).Distinct().Order(StringComparer.Ordinal)];
        ruleChanges.Clear();
        data = loaded.Value.Data;
        View = Viewport.Fit(loaded.Value.Layout.Size.W, loaded.Value.Layout.Size.H, Window);
        Selection = Target.None;
        await Show(loaded.Value.Layout);
    }

    public Task Discard()
    {
        return Kind == GraphKind.Definition ? Import(source) : Load();
    }

    public void SetRuleDraft(RuleDraft draft)
    {
        RuleDraft = draft;
    }

    public Task FlipAccess()
    {
        return Selection.EdgeId is { } edgeId ? OnOneRule(edgeId, FlipRule) : Task.CompletedTask;
    }

    public async Task FlipRule(string edgeId)
    {
        if (Kind != GraphKind.Definition)
        {
            return;
        }
        ChangeAccessRequest request = new(definition, ruleChanges, data, Shape, edgeId);
        Result<ChangeAccessResponse> changed = await Bus.Send(request);
        if (changed.IsSuccess)
        {
            await ApplyRules(changed.Value.Changes, changed.Value.Data, changed.Value.Layout);
        }
    }

    public async Task ExportChanges()
    {
        LayoutData app = Kind == GraphKind.App ? data : boards[GraphKind.App].Data;
        ExportChangesRequest request = new(definition, ruleChanges, changes, app);
        Result<ExportChangesResponse> exported = await Bus.Send(request);
        Export = exported.IsSuccess ? new ExportText(exported.Value.Json, exported.Value.Code) : null;
        await Notify();
    }

    public Task CloseExport()
    {
        Export = null;
        return Notify();
    }

    public async Task Copy(string text)
    {
        Result<CopyTextResponse> copied = await Bus.Send(new CopyTextRequest(text));
        Message = copied.IsSuccess && copied.Value.Copied ? "Copied." : "The browser did not allow the copy.";
        await Notify();
    }

    public async Task RemoveRule(string edgeId)
    {
        RemoveRuleRequest request = new(definition, ruleChanges, data, Shape, edgeId);
        Result<RemoveRuleResponse> removed = await Bus.Send(request);
        if (removed.IsSuccess)
        {
            await ApplyRules(removed.Value.Changes, removed.Value.Data, removed.Value.Layout);
        }
    }

    private async Task AddRule(Target from, Target to)
    {
        AddRuleRequest request = new(
            definition, ruleChanges, data, Shape, Mapping.FocusOf(from), Mapping.FocusOf(to),
            RuleDraft.Access, RuleDraft.Condition, RuleDraft.Layer);
        Result<AddRuleResponse> added = await Bus.Send(request);
        if (added.IsSuccess)
        {
            await ApplyRules(added.Value.Changes, added.Value.Data, added.Value.Layout);
        }
    }

    private Task OnOneRule(string edgeId, Func<string, Task> act)
    {
        if (Links.FirstOrDefault(l => l.Id == edgeId) is not { Merged: true })
        {
            return act(edgeId);
        }
        Message = "Select a rule in the details panel.";
        return Notify();
    }

    private async Task ApplyRules(IReadOnlyList<RuleChange> made, LayoutData changed, LayoutData shown)
    {
        Message = null;
        ruleChanges.Clear();
        ruleChanges.AddRange(made);
        data = changed;
        if (Export is not null)
        {
            await ExportChanges();
        }
        await Show(shown);
    }
}
