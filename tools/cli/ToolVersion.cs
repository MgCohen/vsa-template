using Umpire.Engine.Versions;

namespace Umpire.Cli;

static class ToolVersion {
    public static string Current { get; } = PackageVersion.Of(typeof(ToolVersion).Assembly);
}
