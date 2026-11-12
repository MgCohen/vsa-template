using ArchitectureViewer.Domain.Definitions;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Slices.AddRule;

internal static class NewRule
{
    public static RuleChange? Of(AddRuleRequest request, INodeShape shape)
    {
        Node? source = request.Data.Nodes.FirstOrDefault(n => n.Id == request.From.NodeId);
        Node? target = request.Data.Nodes.FirstOrDefault(n => n.Id == request.To.NodeId);
        if (source is null || target is null || source == target)
        {
            return null;
        }
        bool fromOut = Ports.Named(shape.Shape(source), request.From.PortId)?.Side == PortSide.Out;
        bool toIn = Ports.Named(shape.Shape(target), request.To.PortId)?.Side == PortSide.In;
        if (!fromOut || !toIn)
        {
            return null;
        }
        Term from = DraftTerms.From(source.Layer, request.Condition, request.ConditionLayer);
        Term to = DraftTerms.To(target.Layer, request.Condition, request.ConditionLayer);
        return new RuleChange(RuleOp.AddRule, from, to, request.Access, FromPart: source.Id, ToPart: target.Id);
    }

    public static bool Exists(IReadOnlyList<RuleChange> changes, RuleChange change)
    {
        return changes.Any(c => c.Op == RuleOp.AddRule && Text(c) == Text(change));
    }

    private static string Text(RuleChange change)
    {
        return $"{TermText.Of(change.From)}>{TermText.Of(change.To)}";
    }
}
