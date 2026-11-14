namespace VSA.Architecture.Data;

public sealed record OptionsData(
    string DefaultPermission, string Boundary, bool UseSameLayer, string Depth, string Access);
