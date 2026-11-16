namespace Umpire.Cli.Scaffolding.Setup;

abstract class SetupStep {
    public abstract string Id { get; }

    public abstract SetupResult Plan(SetupContext context);

    protected static SetupResult Done() => new SetupResult.Done();

    protected static SetupResult Change(string file, string content) => new SetupResult.Change(file, content);

    protected static SetupResult ByHand(string message) => new SetupResult.ByHand(message);
}
