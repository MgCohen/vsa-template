namespace PackVariant.Features.Inventory.Slices.AddItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class AddItemHandler : CommandHandler<AddItemCommand, AddItemResult> {
    public override Task<Result<AddItemResult>> HandleAsync(IEnvelope<AddItemCommand> envelope) =>
        Task.FromResult<Result<AddItemResult>>(new AddItemResult(1));
}
