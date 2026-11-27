namespace Operations.Contracts.Commands;

using Operations.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public abstract record JobOperation<TResult> : ICommand<TResult>, IJobOperation;
