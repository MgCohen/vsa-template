namespace VSA.Architecture.Data;

public sealed record RuleNode(List<TermNode> From, RuleVerb Verb, List<TermNode> To);
