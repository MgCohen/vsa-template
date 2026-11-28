namespace Umpire.Vsa.Composition;

using System.Reflection;
using Umpire.Kernels;

public static class RequestNames {
    const string GroupMember = "Group";
    const string NameMember = "Name";
    const BindingFlags Statics = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    public static string Of(Type request, AppTypes types) => $"{GroupOf(request, types)}/{NameOf(request)}";

    public static string Kebab(string name) {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++) {
            if (char.IsUpper(name[i]) && i > 0) {
                text.Append('-');
            }
            text.Append(char.ToLowerInvariant(name[i]));
        }
        return text.ToString();
    }

    public static string NameOf(Type request) =>
        StaticText(request, NameMember) ?? Kebab(request.Name.EndsWith("Request", StringComparison.Ordinal)
            ? request.Name.Substring(0, request.Name.Length - "Request".Length)
            : request.Name);

    public static string GroupOf(Type request, AppTypes types) {
        if (StaticText(request, GroupMember) is { } group) {
            return group;
        }
        var space = request.Namespace ?? "";
        var module = types.Modules
            .Select(m => m.GetType())
            .Where(root => root.Namespace is { } name && (space == name || space.StartsWith(name + ".", StringComparison.Ordinal)))
            .OrderByDescending(root => root.Namespace!.Length)
            .FirstOrDefault();
        return Kebab(module is null ? AppTypes.NameOf(request) : AppTypes.NameOf(module));
    }

    static string? StaticText(Type request, string member) =>
        request.GetProperty(member, Statics) is { } property && property.GetValue(null) is string text ? text : null;
}
