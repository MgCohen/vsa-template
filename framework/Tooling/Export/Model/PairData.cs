namespace Viewer.Export.Model;

public sealed record PairData(
    string From, string To, string Condition, string Access, string Rule, int Order, string MemberAccess);
