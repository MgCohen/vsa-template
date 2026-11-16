namespace PackVariant.App.Host;

using PackVariant.Packs.Ecs;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public class PackVariantApp : App<Ecs<VsaHost>>;
