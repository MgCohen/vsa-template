namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class RunnerTests {
    [Test]
    public void A_runner_that_the_pipeline_cannot_create_gives_its_name_and_the_fix() {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder<Lonely>().CallsTestHandlers().Runner<NeedyRunner>();

        var error = Assert.Throws<InvalidOperationException>(() => builder.Build(provider));

        var start = $"The pipeline of {nameof(Lonely)} cannot create its runner {typeof(NeedyRunner).FullName}: ";
        Assert.That(error.Message, Does.StartWith(start)
            .And.EndWith("Register each service of the runner constructor, or give the runner a public constructor."));
    }

    public sealed class Lonely;

    public sealed class Missing;

    public sealed class NeedyRunner(Missing missing) : IRunner {
        public Missing Missing => missing;

        public Task RunAsync(Envelope envelope, Func<Envelope, Task> run) => run(envelope);
    }
}
