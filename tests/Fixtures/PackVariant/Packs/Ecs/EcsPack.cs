namespace PackVariant.Packs.Ecs;

using PackVariant.Packs.Ecs.Architecture;
using Umpire.Vsa;
using Umpire.Vsa.Composition;

public class Ecs<THost> : Vsa<EcsArchitecture, THost> where THost : VsaHost, new();
