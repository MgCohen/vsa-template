using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.ChangeAccess;

internal sealed class ChangeAccessHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<ChangeAccessRequest, ChangeAccessResponse>
{
    public override async Task<Result<ChangeAccessResponse>> HandleAsync(Envelope<ChangeAccessRequest> envelope)
    {
        ChangeAccessRequest request = envelope.Message;
        RuleEdge? edge = RuleEdges.Of(request.Definition, request.Changes).FirstOrDefault(e => e.Id == request.EdgeId);
        if (edge is null || edge.Removed)
        {
            return new Error("viewer.rule_not_found", $"No access {request.EdgeId}.");
        }
        List<RuleChange> changes = Flipped(request.Changes, edge);
        LayoutData data = DefinitionGraph.Refresh(request.Data, request.Definition, changes);
        return new ChangeAccessResponse(changes, data, await engine.Draw(data, shapes.Named(request.Shape)));
    }

    private static List<RuleChange> Flipped(IReadOnlyList<RuleChange> changes, RuleEdge edge)
    {
        string flipped = AccessRules.Flip(edge.Access);
        return edge.Change switch
        {
            { Op: RuleOp.AddRule } added => [.. changes.Select(c => c == added ? added with { Access = flipped } : c)],
            { Op: RuleOp.ChangeAccess } changed => [.. changes.Where(c => c != changed)],
            _ => [.. changes, new RuleChange(RuleOp.ChangeAccess, edge.From, edge.To, flipped, edge.RuleId)],
        };
    }
}
