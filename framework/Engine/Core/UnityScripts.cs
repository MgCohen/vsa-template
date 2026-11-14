namespace VSA.Architecture.Engine;

public sealed record UnityScripts(IReadOnlyList<string> Files, RuleFinding? Warning) {
    public static readonly UnityScripts None = new([], null);
}
