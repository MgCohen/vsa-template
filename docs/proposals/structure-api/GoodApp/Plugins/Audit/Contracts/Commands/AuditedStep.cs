using VSA.Contracts.Commands;

namespace GoodApp.Plugins.Audit.Contracts.Commands;

public abstract record AuditedStep<TResult> : ICommand<TResult>;
