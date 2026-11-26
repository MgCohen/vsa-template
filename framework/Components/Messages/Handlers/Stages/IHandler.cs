namespace Umpire.Messages.Handlers;

public interface IHandler;

public interface IHandler<in TMessage> : IHandler where TMessage : class;
