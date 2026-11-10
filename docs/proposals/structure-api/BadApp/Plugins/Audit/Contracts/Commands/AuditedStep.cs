using VSA.Contracts.Commands;

namespace BadApp.Plugins.Audit.Contracts.Commands;

public abstract record AuditedStep<TResult> : ICommand<TResult>;
