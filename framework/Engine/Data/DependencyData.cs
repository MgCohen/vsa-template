namespace VSA.Architecture.Data;

public sealed record DependencyData(string From, string To, string Access, List<UseData> Uses);
