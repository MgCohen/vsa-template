namespace Umpire.Vsa.Presenters;

using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Events;

public static class PresenterPipelineBuilderExtensions {
    public static PipelineBuilder<TMessage> CallsPresenters<TMessage>(this PipelineBuilder<TMessage> builder)
        where TMessage : class =>
        builder.CallsEventHandlers().AfterHandled<Presenter>(presenter => presenter.RaiseChanged());
}
