using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Steps.Handlers;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Module;

namespace Steps;

public sealed class StepsPlugin : Plugin {
    public override void Configure(ModuleBuilder module) {
        module.Services.AddScoped<StepLog>();
        module.Services.AddPipeline(new PipelineBuilder<IEngineStep>()
            .HandlersOf(typeof(CommandHandler<,>))
            .Runner<StepRunner>()
            .AfterStep<RecordStep>());
    }
}
