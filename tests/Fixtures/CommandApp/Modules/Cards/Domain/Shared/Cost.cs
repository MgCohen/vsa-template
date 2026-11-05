using Resources.Domain.Shared;

namespace Cards.Domain.Shared;

// The published domain of Cards. It holds a key of Resources, a module in VsaModuleUses.
public sealed record Cost(ResourceKey Resource, int Amount);
