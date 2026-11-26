namespace Umpire.Messages.Handlers;

sealed record AfterHandledCall(Type Handler, Action<object> Call);
