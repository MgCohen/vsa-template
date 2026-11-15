namespace Umpire.Cli;

using Umpire.Engine.Versions;

static class ToolVersion {
    public static string Current { get; } = PackageVersion.Of(typeof(ToolVersion).Assembly);
}
