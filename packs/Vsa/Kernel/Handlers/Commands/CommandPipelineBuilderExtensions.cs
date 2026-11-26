namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Messages.Handlers;

public static class CommandPipelineBuilderExtensions {
    public static PipelineBuilder<TMessage> CallsCommandHandlers<TMessage>(this PipelineBuilder<TMessage> builder)
        where TMessage : class =>
        builder.Calls(typeof(CommandHandlerCall<,>));
}
