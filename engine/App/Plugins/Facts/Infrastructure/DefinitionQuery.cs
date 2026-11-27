namespace Facts.Infrastructure;

public sealed record DefinitionQuery(string Bin, IReadOnlyList<string> References);
