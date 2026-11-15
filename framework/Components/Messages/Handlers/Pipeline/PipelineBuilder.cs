namespace VSA.Handlers;

public sealed class PipelineBuilder<TMessage> where TMessage : class {
    readonly List<Type> handlerBases = [];
    readonly List<Type> handlers = [];
    readonly List<Type> hookBases = [];
    readonly List<Type> beforeSteps = [];
    readonly List<Type> afterSteps = [];
    Type runner = typeof(InlineRunner);

    public PipelineBuilder<TMessage> Handlers<THandlerBase>() where THandlerBase : class =>
        HandlersOf(typeof(THandlerBase));

    public PipelineBuilder<TMessage> HandlersOf(Type handlerBase) {
        handlerBases.Add(handlerBase ?? throw new ArgumentNullException(nameof(handlerBase)));
        return this;
    }

    public PipelineBuilder<TMessage> Handlers(params Type[] handlers) {
        foreach (var handler in handlers ?? throw new ArgumentNullException(nameof(handlers))) {
            MessageTypes.OfHandler(handler);
            this.handlers.Add(handler);
        }
        return this;
    }

    public PipelineBuilder<TMessage> HooksOf(params Type[] hookBases) {
        foreach (var hookBase in hookBases ?? throw new ArgumentNullException(nameof(hookBases))) {
            if (!IsHookBase(hookBase)) {
                throw new ArgumentException(
                    $"{hookBase?.FullName ?? "null"} is not a hook base. "
                    + "A hook base implements IBeforeCall or IAfterCall, and it is not a step base. "
                    + "Add a step with BeforeStep<TStep>() or AfterStep<TStep>().",
                    nameof(hookBases));
            }
            this.hookBases.Add(hookBase);
        }
        return this;
    }

    public PipelineBuilder<TMessage> Runner<TRunner>() where TRunner : IRunner {
        runner = typeof(TRunner);
        return this;
    }

    public PipelineBuilder<TMessage> BeforeStep<TStep>() where TStep : BeforeStep {
        AddOnce(beforeSteps, typeof(TStep), "a before");
        return this;
    }

    public PipelineBuilder<TMessage> AfterStep<TStep>() where TStep : AfterStep {
        AddOnce(afterSteps, typeof(TStep), "an after");
        return this;
    }

    public Pipeline<TMessage> Build(IServiceProvider services) =>
        new(Definition(), services ?? throw new ArgumentNullException(nameof(services)));

    static bool IsHookBase(Type? type) =>
        type is not null
        && (typeof(IBeforeCall).IsAssignableFrom(type) || typeof(IAfterCall).IsAssignableFrom(type))
        && !IsStep(type);

    static bool IsStep(Type type) =>
        typeof(BeforeStep).IsAssignableFrom(type) || typeof(AfterStep).IsAssignableFrom(type);

    static void AddOnce(List<Type> steps, Type step, string stage) {
        if (steps.Contains(step)) {
            throw new InvalidOperationException(
                $"{step.FullName} is already {stage} step of the pipeline of {typeof(TMessage).Name}. "
                + "Add each step one time.");
        }
        steps.Add(step);
    }

    PipelineDefinition Definition() =>
        new(typeof(TMessage), [.. handlerBases], [.. handlers], [.. hookBases], runner,
            [.. beforeSteps], [.. afterSteps]);
}
