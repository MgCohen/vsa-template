using Resources.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

namespace Resources.Slices.ChangeResource;

public sealed record ChangeResourceCommand(ResourceKey Resource, int Delta) : ICommand<ChangeResourceResult>;
