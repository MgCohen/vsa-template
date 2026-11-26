namespace Umpire.Vsa.Handlers.Requests;

using Umpire.Messages.Handlers;

public static class RequestPipelineBuilderExtensions {
    public static PipelineBuilder<TMessage> CallsRequestHandlers<TMessage>(this PipelineBuilder<TMessage> builder)
        where TMessage : class =>
        builder.Calls(typeof(RequestHandlerCall<,>));
}
