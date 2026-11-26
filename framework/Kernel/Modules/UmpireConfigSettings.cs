namespace Umpire.Kernels;

using Umpire.Config;

public sealed class UmpireConfigSettings(UmpireConfig config) : ISettingsProvider {
    public TSettings? Get<TSettings>(Module module) where TSettings : class, new() =>
        config.Get<TSettings>(module.Name);
}
