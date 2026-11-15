namespace VSA.Composition;

using VSA.Kernel;

public abstract class Plugin : Module {
    protected internal override int Order => 0;
}
