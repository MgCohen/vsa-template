namespace Arena.Slices.Finisher;

using Umpire.Vsa.Handlers.Commands;

public abstract class FinisherHandler<TCommand> : CommandHandler<TCommand, int> where TCommand : FinisherMove;
