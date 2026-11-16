namespace Umpire.Vsa.Contracts.Commands;

using Umpire.Messages.Contracts;

public interface ICommand<TResult> : ICommand;

public interface ICommand : IMessage;
