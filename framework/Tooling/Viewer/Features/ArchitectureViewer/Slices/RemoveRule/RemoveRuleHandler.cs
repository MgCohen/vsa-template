using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.RemoveRule;

internal sealed class RemoveRuleHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<RemoveRuleRequest, RemoveRuleResponse>
{
    public override async Task<Result<RemoveRuleResponse>> HandleAsync(Envelope<RemoveRuleRequest> envelope)
    {
        RemoveRuleRequest request = envelope.Message;
        RuleEdge? edge = RuleEdges.Of(request.Definition, request.Changes).FirstOrDefault(e => e.Id == request.EdgeId);
        if (edge is null)
        {
            return new Error("viewer.rule_not_found", $"No access {request.EdgeId}.");
        }
        List<RuleChange> changes = [.. request.Changes.Where(c => c != edge.Change)];
        if (edge.RuleId is { } ruleId && !edge.Removed)
        {
            string access = request.Definition.Rules.First(r => r.Id == ruleId).Access;
            changes.Add(new RuleChange(RuleOp.RemoveRule, edge.From, edge.To, access, ruleId));
        }
        LayoutData data = DefinitionGraph.Refresh(request.Data, request.Definition, changes);
        return new RemoveRuleResponse(changes, data, await engine.Draw(data, shapes.Named(request.Shape)));
    }
}
