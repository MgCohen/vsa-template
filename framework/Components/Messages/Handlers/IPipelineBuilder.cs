namespace VSA.Handlers;

public interface IPipelineBuilder<TMessage> where TMessage : class {
    IPipelineBuilder<TMessage> Handlers<THandlerBase>() where THandlerBase : class;

    IPipelineBuilder<TMessage> HandlersOf(Type handlerBase);

    IPipelineBuilder<TMessage> Handlers(params Type[] handlers);

    IPipelineBuilder<TMessage> Runner<TRunner>() where TRunner : IRunner;

    IPipelineBuilder<TMessage> BeforeStep<TStep>() where TStep : class;

    IPipelineBuilder<TMessage> AfterStep<TStep>() where TStep : class;

    IPipeline<TMessage> Build(IServiceProvider services);
}
