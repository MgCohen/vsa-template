namespace Umpire.Messages.Handlers;

public sealed class PipelineBuilder<TMessage> where TMessage : class {
    readonly List<Type> handlerBases = [];
    readonly List<Type> handlers = [];
    readonly List<Type> hookBases = [];
    readonly List<Type> validatorBases = [];
    readonly List<PipelineStep> beforeSteps = [];
    readonly List<PipelineStep> afterSteps = [];
    readonly List<Type> calls = [];
    readonly List<AfterHandledCall> afterHandled = [];
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

    public PipelineBuilder<TMessage> ValidatorsOf(params Type[] validatorBases) {
        foreach (var validatorBase in validatorBases ?? throw new ArgumentNullException(nameof(validatorBases))) {
            if (!IsValidatorBase(validatorBase)) {
                throw new ArgumentException(
                    $"{validatorBase?.FullName ?? "null"} is not a validator base. "
                    + "A validator base is an open generic type with one type parameter, the message, "
                    + "and it implements IValidation, for example Validator<>.",
                    nameof(validatorBases));
            }
            if (!this.validatorBases.Contains(validatorBase)) {
                this.validatorBases.Add(validatorBase);
            }
        }
        return this;
    }

    public PipelineBuilder<TMessage> Calls(params Type[] calls) {
        foreach (var call in calls ?? throw new ArgumentNullException(nameof(calls))) {
            if (!IsCall(call)) {
                throw new ArgumentException(
                    $"{call?.FullName ?? "null"} is not a handler call. "
                    + "A handler call is a concrete open generic type with one type parameter, the message, "
                    + "a public constructor with no parameters, and the base HandlerCall<THandler, TMessage>.",
                    nameof(calls));
            }
            if (!this.calls.Contains(call)) {
                this.calls.Add(call);
            }
        }
        return this;
    }

    public PipelineBuilder<TMessage> AfterHandled<THandler>(Action<THandler> call) where THandler : class {
        if (call is null) {
            throw new ArgumentNullException(nameof(call));
        }
        afterHandled.Add(new AfterHandledCall(typeof(THandler), handler => call((THandler)handler)));
        return this;
    }

    public PipelineBuilder<TMessage> Runner<TRunner>() where TRunner : IRunner {
        runner = typeof(TRunner);
        return this;
    }

    public PipelineBuilder<TMessage> BeforeStep<TStep>(int priority = Pipeline.ValidationPriority)
        where TStep : BeforeStep {
        AddOnce(beforeSteps, new PipelineStep(typeof(TStep), priority), "a before");
        return this;
    }

    public PipelineBuilder<TMessage> AfterStep<TStep>(int priority = Pipeline.ValidationPriority)
        where TStep : AfterStep {
        AddOnce(afterSteps, new PipelineStep(typeof(TStep), priority), "an after");
        return this;
    }

    public Pipeline Build(IServiceProvider services) =>
        new(Definition(), services ?? throw new ArgumentNullException(nameof(services)));

    internal PipelineDefinition Definition() =>
        new(typeof(TMessage), [.. handlerBases], [.. handlers], [.. hookBases],
            validatorBases.Count > 0 ? [.. validatorBases] : [typeof(Validator<>)], runner,
            [.. beforeSteps], [.. afterSteps], [.. calls, typeof(RunCall<>)], [.. afterHandled]);

    static bool IsValidatorBase(Type? type) =>
        type is { IsGenericTypeDefinition: true }
        && type.GetGenericArguments().Length == 1
        && typeof(IValidation).IsAssignableFrom(type);

    static bool IsCall(Type? type) =>
        type is { IsGenericTypeDefinition: true, IsAbstract: false }
        && type.GetGenericArguments().Length == 1
        && type.GetConstructor(Type.EmptyTypes) is not null
        && typeof(IHandlerCall).IsAssignableFrom(type);

    static bool IsHookBase(Type? type) =>
        type is not null
        && (typeof(IBeforeCall).IsAssignableFrom(type) || typeof(IAfterCall).IsAssignableFrom(type))
        && !IsStep(type);

    static bool IsStep(Type type) =>
        typeof(BeforeStep).IsAssignableFrom(type) || typeof(AfterStep).IsAssignableFrom(type);

    static void AddOnce(List<PipelineStep> steps, PipelineStep step, string stage) {
        if (steps.Exists(added => added.Step == step.Step)) {
            throw new InvalidOperationException(
                $"{step.Step.FullName} is already {stage} step of the pipeline of {typeof(TMessage).Name}. "
                + "Add each step one time.");
        }
        steps.Add(step);
    }
}
