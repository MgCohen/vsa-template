namespace Umpire.Messages.Contracts;

public interface IHandler;

public interface IHandler<in TMessage> : IHandler where TMessage : class;
