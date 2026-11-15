using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using VSA.Composition;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Kernel;

namespace Steps;

public sealed class StepsPlugin : Plugin {
    public override void Configure(IServiceCollection services) {
        services.AddScoped<StepLog>();
        services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StepRunner>()
            .BeforeStep<MissingInput>()
            .AfterStep<RecordStep>());
    }
}
