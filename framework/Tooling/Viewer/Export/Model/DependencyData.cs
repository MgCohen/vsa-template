namespace Viewer.Export.Model;

public sealed record DependencyData(string From, string To, string Access, List<UseData> Uses);
