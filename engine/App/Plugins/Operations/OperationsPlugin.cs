namespace Operations;

using Microsoft.Extensions.DependencyInjection;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Vsa.Composition;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;

public sealed class OperationsPlugin : Plugin {
    protected override void Configure(IServiceCollection services) {
        services.AddScoped<OperationLog>();
        services.AddScoped<RunOptions>();
        services.AddPipeline(new PipelineBuilder<IJobOperation>()
            .HandlersOf(typeof(CommandHandler<,>))
            .CallsCommandHandlers()
            .Runner<OperationRunner>()
            .AfterStep<RecordOperation>());
    }
}
