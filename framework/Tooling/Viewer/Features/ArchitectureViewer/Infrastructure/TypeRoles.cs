namespace ArchitectureViewer.Infrastructure;

public static class TypeRoles
{
    private static readonly (string Role, string? Layer, string[] Endings)[] roles =
    [
        ("handler", null, ["Handler"]),
        ("request", null, ["Request", "Command"]),
        ("response", null, ["Response", "Result"]),
        ("event", "Contracts.Events", ["Created", "Deleted", "Updated", "Event"]),
        ("store", null, ["Store", "Repository"]),
        ("module", "RootClass", ["Module"]),
        ("view", "Views", ["Form", "View", "Page", "Presenter", "Item"]),
    ];

    public static string Of(string name, string layer)
    {
        foreach ((string role, string? roleLayer, string[] endings) in roles)
        {
            if (layer == roleLayer || endings.Any(e => name.EndsWith(e, StringComparison.Ordinal)))
            {
                return role;
            }
        }
        return "data";
    }
}
