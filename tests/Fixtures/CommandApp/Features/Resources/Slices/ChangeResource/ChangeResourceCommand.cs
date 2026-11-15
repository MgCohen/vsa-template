namespace Resources.Slices.ChangeResource;

using Resources.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record ChangeResourceCommand(ResourceKey Resource, int Delta) : ICommand<ChangeResourceResult>;
