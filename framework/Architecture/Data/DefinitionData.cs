namespace VSA.Architecture.Data;

public sealed record DefinitionData(
    string Name, OptionsData Options, List<PartData> Parts, List<RuleData> Rules, List<PairData> Pairs);
