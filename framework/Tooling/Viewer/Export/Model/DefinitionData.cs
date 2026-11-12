namespace Viewer.Export.Model;

public sealed record DefinitionData(
    string Name, string Unlisted, string Own, List<PartData> Parts, List<RuleData> Rules, List<PairData> Pairs);
