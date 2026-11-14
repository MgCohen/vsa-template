using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace ArchitectureViewer.Slices.AddRule;

internal sealed class AddRuleHandler(ILayoutEngine engine, IEnumerable<INodeShape> shapes)
    : RequestHandler<AddRuleRequest, AddRuleResponse>
{
    public override async Task<Result<AddRuleResponse>> HandleAsync(Envelope<AddRuleRequest> envelope)
    {
        AddRuleRequest request = envelope.Message;
        INodeShape shape = shapes.Named(request.Shape);
        RuleChange? change = NewRule.Of(request, shape);
        if (change is null || NewRule.Exists(request.Changes, change))
        {
            return new Error("viewer.rule_rejected", "A rule goes from an Out port to an In port of another part.");
        }
        IReadOnlyList<RuleChange> changes = [.. request.Changes, change];
        LayoutData data = DefinitionGraph.Refresh(request.Data, request.Definition, changes);
        return new AddRuleResponse(changes, data, await engine.Draw(data, shape));
    }
}
