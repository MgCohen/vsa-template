using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain;

public static class AccessRules
{
    public static string Worst(IEnumerable<string> values)
    {
        List<string> all = [.. values];
        return new[] { Access.Deny, Access.Unchecked, Access.Conditional }.FirstOrDefault(all.Contains) ?? Access.Allow;
    }

    public static string Flip(string access)
    {
        return access == Access.Deny ? Access.Allow : Access.Deny;
    }

    public static string Verb(string access)
    {
        return access == Access.Deny ? "denies" : "uses";
    }
}
