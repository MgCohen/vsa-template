using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Commands;
using VSA.Module;

namespace Composition.Tests;

public sealed class OneAssemblyModuleTests {
    [Test]
    public void The_scan_reads_the_handlers_in_the_root_assembly_of_a_module() {
        var types = new ModuleTypes([new OneAssemblyModule()]);

        var handlers = types.Implementations(typeof(CommandHandler<,>)).Select(found => found.Implementation);

        Assert.That(handlers, Does.Contain(typeof(OneAssemblyHandler)));
    }

    public sealed class OneAssemblyModule : VSA.Module.Module {
        public override void Configure(ModuleBuilder module) {
        }
    }

    public sealed record OneAssemblyCommand : ICommand<int>;

    public sealed class OneAssemblyHandler : CommandHandler<OneAssemblyCommand, int> {
        public override Task<Result<int>> HandleAsync(Envelope<OneAssemblyCommand> envelope) => Task.FromResult<Result<int>>(1);
    }
}
