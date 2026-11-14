namespace VSA.Handlers;

public sealed class PipelineBuilder<TMessage> : IPipelineBuilder<TMessage> where TMessage : class {
    readonly List<Type> handlerBases = [];
    readonly List<Type> handlers = [];
    readonly List<Type> beforeSteps = [];
    readonly List<Type> afterSteps = [];
    Type runner = typeof(InlineRunner);

    public IPipelineBuilder<TMessage> Handlers<THandlerBase>() where THandlerBase : class =>
        HandlersOf(typeof(THandlerBase));

    public IPipelineBuilder<TMessage> HandlersOf(Type handlerBase) {
        handlerBases.Add(handlerBase ?? throw new ArgumentNullException(nameof(handlerBase)));
        return this;
    }

    public IPipelineBuilder<TMessage> Handlers(params Type[] handlers) {
        foreach (var handler in handlers ?? throw new ArgumentNullException(nameof(handlers))) {
            MessageTypes.OfHandler(handler);
            this.handlers.Add(handler);
        }
        return this;
    }

    public IPipelineBuilder<TMessage> Runner<TRunner>() where TRunner : IRunner {
        runner = typeof(TRunner);
        return this;
    }

    public IPipelineBuilder<TMessage> BeforeStep<TStep>() where TStep : class {
        MessageTypes.OfStep(typeof(TStep), typeof(BeforeStep<>));
        beforeSteps.Add(typeof(TStep));
        return this;
    }

    public IPipelineBuilder<TMessage> AfterStep<TStep>() where TStep : class {
        MessageTypes.OfStep(typeof(TStep), typeof(AfterStep<>));
        afterSteps.Add(typeof(TStep));
        return this;
    }

    public IPipeline<TMessage> Build(IServiceProvider services) =>
        new Pipeline<TMessage>(Definition(), services ?? throw new ArgumentNullException(nameof(services)));

    PipelineDefinition Definition() =>
        new(typeof(TMessage), [.. handlerBases], [.. handlers], runner, [.. beforeSteps], [.. afterSteps]);
}
