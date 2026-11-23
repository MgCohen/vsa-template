namespace PackVariant.Features.Inventory.Slices.AddItem;

using Umpire.Vsa.Contracts.Commands;

public sealed record AddItemCommand(string Name) : ICommand<AddItemResult>;
