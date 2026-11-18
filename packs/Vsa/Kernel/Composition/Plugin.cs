namespace Umpire.Vsa.Composition;

using Umpire.Kernels;

public abstract class Plugin : Module {
    protected internal override int Order => 0;
}
