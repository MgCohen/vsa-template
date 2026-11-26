namespace Umpire.Vsa.Handlers.Events;

using Umpire.Messages.Handlers;

public static class EventPipelineBuilderExtensions {
    public static PipelineBuilder<TMessage> CallsEventHandlers<TMessage>(this PipelineBuilder<TMessage> builder)
        where TMessage : class =>
        builder.Calls(typeof(EventHandlerCall<>));
}
