namespace Umpire.Cli.Scaffolding.Setup;

abstract class SetupStep {
    public abstract string Id { get; }

    public abstract IReadOnlyList<string> Files(SetupContext context);

    public abstract SetupResult Plan(SetupContext context);

    protected static SetupResult Done() => new SetupResult.Done();

    protected static SetupResult Change(string file, string content) => new SetupResult.Change(file, content);

    protected static SetupResult ByHand(string message) => new SetupResult.ByHand(message);

    protected static SetupResult ChangeOrByHand(string file, string? content, string message) =>
        content is null ? ByHand(message) : Change(file, content);
}
