namespace Umpire.Cli;

using Umpire.Architecture;

public sealed record ConfigLibraries : External {
    public override string Assembly => "Umpire.Config";
}
