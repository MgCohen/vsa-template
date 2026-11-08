namespace VSA.Module;

// A plugin module with settings. The host binds Modules:{Name} to TSettings, the same as for Module<TSettings>.
public abstract class Plugin<TSettings> : Plugin where TSettings : class, new() {
    public sealed override void Configure(ModuleBuilder module) => Configure(module, module.Settings<TSettings>(Validate));

    public abstract void Configure(ModuleBuilder module, TSettings settings);

    // Rules that data annotations cannot express, for example rules over two fields. Return one message for each error.
    protected virtual IEnumerable<string> Validate(TSettings settings) => [];
}
