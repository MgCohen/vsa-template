namespace Umpire.Kernels;

using Tomlyn.Serialization;
using Umpire.Config;

public sealed class UmpireConfigSettings(UmpireConfig config, TomlSerializerContext? context = null) : ISettingsProvider {
    public TSettings? Get<TSettings>(Module module) where TSettings : class, new() =>
        config.Get<TSettings>(module.Name, context);
}
