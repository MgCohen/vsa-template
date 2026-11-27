namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

abstract class SetupTask {
    public abstract string Id { get; }

    public abstract SetupResult Plan(SetupContext context);
}
