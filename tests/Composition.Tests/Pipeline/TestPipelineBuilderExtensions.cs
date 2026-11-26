namespace Composition.Tests.Pipeline;

using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;

public static class TestPipelineBuilderExtensions {
    public static PipelineBuilder<TMessage> CallsTestHandlers<TMessage>(this PipelineBuilder<TMessage> builder)
        where TMessage : class =>
        builder
            .Calls(typeof(TestHandlerCall<>))
            .CallsCommandHandlers()
            .CallsRequestHandlers()
            .CallsEventHandlers();
}
