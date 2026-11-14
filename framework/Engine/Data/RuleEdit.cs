namespace VSA.Architecture.Data;

public sealed record RuleEdit(
    RuleEditKind Kind,
    string From,
    string To,
    RuleVerb Verb = RuleVerb.Uses,
    string? Rule = null,
    IReadOnlyList<NamedFilter>? FromFilters = null,
    IReadOnlyList<NamedFilter>? ToFilters = null);
