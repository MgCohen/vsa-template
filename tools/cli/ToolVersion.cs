namespace Umpire.Cli;

using System.Reflection;
using Umpire.Engine.Versions;

static class ToolVersion {
    public static string Current { get; } = PackageVersion.Of(typeof(ToolVersion).Assembly);

    public static string NUnit { get; } = Metadata("UmpireNUnitVersion");

    public static string NUnitAdapter { get; } = Metadata("UmpireNUnitAdapterVersion");

    static string Metadata(string key) =>
        typeof(ToolVersion).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?.Value
        ?? throw new UmpireException($"The tool has no {key}. Report this error to the Umpire maintainers.");
}
