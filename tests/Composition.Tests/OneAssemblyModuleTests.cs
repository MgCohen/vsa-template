using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernel;

namespace Composition.Tests;

public sealed class OneAssemblyModuleTests {
    [Test]
    public void The_scan_reads_the_handlers_in_the_root_assembly_of_a_module() {
        var types = new AppTypes([new OneAssemblyModule()]);

        var handlers = types.Implementations(typeof(CommandHandler<,>)).Select(found => found.Implementation);

        Assert.That(handlers, Does.Contain(typeof(OneAssemblyHandler)));
    }

    public sealed class OneAssemblyModule : Module {
        public override void Configure(IServiceCollection services) {
        }
    }

    public sealed record OneAssemblyCommand : ICommand<int>;

    public sealed class OneAssemblyHandler : CommandHandler<OneAssemblyCommand, int> {
        public override Task<Result<int>> HandleAsync(Envelope<OneAssemblyCommand> envelope) => Task.FromResult<Result<int>>(1);
    }
}
