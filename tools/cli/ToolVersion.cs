using VSA.Architecture.Versions;

namespace VSA.Cli;

static class ToolVersion {
    public static string Current { get; } = PackageVersion.Of(typeof(ToolVersion).Assembly);
}
