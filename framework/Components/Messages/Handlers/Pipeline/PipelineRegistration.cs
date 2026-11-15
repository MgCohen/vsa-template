namespace Umpire.Messages.Handlers;

sealed record PipelineRegistration(Type Message, Type? HandlerBase, Func<IServiceProvider, IPipelineRun> Build);
