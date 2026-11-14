using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Module;
using VSA.Kernel;

namespace Steps;

public sealed class StepsPlugin : Plugin {
    public override void Configure(AnchorBuilder module) {
        module.Services.AddScoped<StepLog>();
        module.Services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StepRunner>()
            .BeforeStep<MissingInput>()
            .AfterStep<RecordStep>());
    }
}
