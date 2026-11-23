namespace PackVariant.Packs.Ecs;

using PackVariant.Packs.Ecs.Definition;
using Umpire.Kernels;
using Umpire.Vsa;

public class Ecs<THost> : Vsa<EcsArchitecture, THost> where THost : AppHost, new();
