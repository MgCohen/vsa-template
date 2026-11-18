namespace Umpire.Kernels;

public interface ISettingsProvider {
    TSettings? Get<TSettings>(Module module) where TSettings : class, new();
}
