namespace Umpire.Messages.Handlers;

using System.Runtime.ExceptionServices;
using Umpire.Messages.Contracts;

sealed class FirstFault(Envelope envelope) {
    ExceptionDispatchInfo? first;

    internal void Add(Exception exception) {
        if (first is not null) {
            return;
        }
        envelope.Fault(exception);
        first = ExceptionDispatchInfo.Capture(exception);
    }

    internal void ThrowIfAny() => first?.Throw();
}
