namespace ArchitectureViewer.Infrastructure.Elk;

public static class ElkOptions
{
    public static readonly Dictionary<string, string> Root = new()
    {
        ["elk.algorithm"] = "layered",
        ["elk.direction"] = "RIGHT",
        ["elk.hierarchyHandling"] = "INCLUDE_CHILDREN",
        ["elk.layered.spacing.nodeNodeBetweenLayers"] = "70",
        ["elk.spacing.nodeNode"] = "26",
        ["elk.spacing.edgeEdge"] = "8",
        ["elk.edgeRouting"] = "ORTHOGONAL",
        ["elk.layered.considerModelOrder.strategy"] = "NODES_AND_EDGES",
        ["elk.layered.crossingMinimization.forceNodeModelOrder"] = "true",
        ["elk.padding"] = "[top=24,left=24,bottom=24,right=24]",
    };

    public static readonly Dictionary<string, string> Group = new()
    {
        ["elk.padding"] = "[top=44,left=18,bottom=18,right=18]",
        ["elk.layered.crossingMinimization.forceNodeModelOrder"] = "true",
    };

    public static readonly Dictionary<string, string> Card = new() { ["elk.portConstraints"] = "FIXED_POS" };

    public static readonly Dictionary<string, string> West = new() { ["elk.port.side"] = "WEST" };

    public static readonly Dictionary<string, string> East = new() { ["elk.port.side"] = "EAST" };
}
