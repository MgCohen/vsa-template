namespace VSA.Architecture.Data;

public sealed record PairData(
    string From, string To, string Condition, string Access, string Rule, int Order, string MemberAccess);
