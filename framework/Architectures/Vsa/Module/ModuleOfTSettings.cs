using VSA.Kernel;

namespace VSA.Module;

// A module with settings. The host binds Modules:{Name} to TSettings, checks the data annotations
// and Validate, and only then calls Configure. Settings stay data only: the rules are here.
public abstract class Module<TSettings> : Module where TSettings : class, new() {
    public sealed override void Configure(AnchorBuilder module) => Configure(module, module.Settings<TSettings>(Section, Validate));

    public abstract void Configure(AnchorBuilder module, TSettings settings);

    // Rules that data annotations cannot express, for example rules over two fields. Return one message for each error.
    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
