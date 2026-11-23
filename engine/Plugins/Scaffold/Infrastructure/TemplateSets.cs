namespace Scaffold.Infrastructure;

using Scaffold.Domain.Shared;
using Umpire.Architecture;

public static class TemplateSets {
    public const string SamplePrefix = "Sample";
    public const string Group = "Slice";
    const string HandlersPrefix = "Handlers.";

    public static IReadOnlyList<TemplateSet> In(Bindings templates) =>
        [.. templates.All
            .Where(IsSampleSliceType)
            .GroupBy(binding => binding.Path, StringComparer.Ordinal)
            .Select(set => new TemplateSet(KindOf(set), Group, [.. set.OrderBy(Name, StringComparer.Ordinal)]))
            .Where(set => set.Kind.Length > 0)
            .OrderBy(set => set.Kind, StringComparer.Ordinal)];

    static bool IsSampleSliceType(Binding binding) =>
        !binding.External
        && binding.Source is not null
        && binding.Values.TryGetValue(Group, out var token)
        && token.StartsWith(SamplePrefix, StringComparison.Ordinal);

    static string KindOf(IEnumerable<Binding> set) =>
        set.Select(binding => binding.Part.Name).FirstOrDefault(IsHandlerKind) is { } handlers
            ? Singular(handlers[HandlersPrefix.Length..]).ToLowerInvariant()
            : "";

    static bool IsHandlerKind(string part) =>
        part.StartsWith(HandlersPrefix, StringComparison.Ordinal) && part.Count(c => c == '.') == 1;

    static string Name(Binding binding) => binding.Type.Name;

    static string Singular(string name) => name.EndsWith('s') ? name[..^1] : name;
}
