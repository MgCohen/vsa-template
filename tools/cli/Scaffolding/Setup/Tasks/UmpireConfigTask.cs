namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using Umpire.Config;
using Umpire.Engine.Versions;
using Umpire.Files;

sealed class UmpireConfigTask : SetupTask {
    const string FileName = UmpireConfig.FileName;
    static readonly string[] NotSource = ["bin", "obj"];

    public override string Id => "umpire-config";

    public override SetupResult Plan(SetupContext context) =>
        context.Read(FileName) is null && (!context.ExistingApp || Projects(context) >= 2)
            ? new SetupResult.Change(new TextFile(FileName, Templates.SetupFile(FileName, context.Version)))
            : new SetupResult.Done();

    static int Projects(SetupContext context) {
        IEnumerable<string> onDisk = Directory.Exists(context.Block)
            ? Directory.EnumerateFiles(context.Block, "*.csproj", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(context.Block, file).Replace('\\', '/'))
                .Where(file => !file.Split('/').Any(part => NotSource.Contains(part) || part.EndsWith('~')))
            : [];
        return context.Planned.Select(file => file.Path)
            .Where(file => file.EndsWith(".csproj", StringComparison.Ordinal))
            .Concat(onDisk)
            .Distinct(StringComparer.Ordinal)
            .Count();
    }
}
