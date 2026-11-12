using VSA.Architecture.Engine;

namespace VSA.Testing.Architecture;

public static class ArchitectureScope {
    static readonly AsyncLocal<AppArchitecture?> CurrentApp = new();

    public static AppArchitecture Current {
        get => CurrentApp.Value ?? throw new InvalidOperationException("No architecture is loaded. Inherit ArchitectureTests<TDefinition>.");
        set => CurrentApp.Value = value;
    }
}
