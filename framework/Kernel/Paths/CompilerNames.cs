namespace Umpire.Utility;

public static class CompilerNames {
    public static bool IsGenerated(string typeName) => typeName.StartsWith("<", StringComparison.Ordinal);
}
