namespace Umpire.Vsa.Composition;

using Umpire.Kernel;

public abstract class Plugin : Module {
    protected internal override int Order => 0;
}
