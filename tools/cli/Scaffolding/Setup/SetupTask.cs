namespace Umpire.Cli.Scaffolding.Setup;

abstract class SetupTask {
    public abstract string Id { get; }

    public abstract SetupResult Plan(SetupContext context);
}
