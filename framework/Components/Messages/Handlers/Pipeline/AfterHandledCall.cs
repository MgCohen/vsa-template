namespace Umpire.Messages.Handlers;

sealed record AfterHandledCall(Type Handler, Delegate Source, Action<object> Call) {
    internal bool Repeats(Type handler, Delegate source) => Handler == handler && Source.Equals(source);
}
