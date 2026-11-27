namespace Umpire.Cli;

using Umpire.Architecture;

public sealed record VersionLibraries : External {
    public override string Assembly => "Umpire.Engine.Versions";
}
