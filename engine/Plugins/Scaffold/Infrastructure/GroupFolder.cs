namespace Scaffold.Infrastructure;

using Umpire.Architecture;

public sealed record GroupFolder(string Group, string Value, string Folder, string Assembly) {
    public static GroupFolder? Find(Bindings app, string group, string value) =>
        app.All
            .Where(binding => !binding.External && binding.Source is not null)
            .Where(binding => binding.Part.Path.EndsWith(Placeholder(group), StringComparison.Ordinal))
            .Where(binding => HasValue(binding, group, value))
            .Select(binding => new GroupFolder(
                group, binding.Values[group], Path.GetDirectoryName(binding.Source!.File)!, binding.Type.Assembly))
            .FirstOrDefault();

    public static string Placeholder(string group) => $"{{{group.ToLowerInvariant()}}}";

    static bool HasValue(Binding binding, string group, string value) =>
        binding.Values.TryGetValue(group, out var found)
        && string.Equals(found, value, StringComparison.OrdinalIgnoreCase);
}
