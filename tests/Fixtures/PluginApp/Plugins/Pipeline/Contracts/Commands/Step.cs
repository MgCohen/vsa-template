using VSA.Contracts.Commands;

namespace Pipeline.Contracts.Commands;

// The base of the commands of each module that run as one step.
public abstract record Step<TResult> : ICommand<TResult>;
