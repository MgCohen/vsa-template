namespace Spike.Kernels;

public sealed class AppTypes(IModuleProvider provider) {
    public IReadOnlyList<Module> Modules => provider.Modules;

    public IReadOnlyList<Implementation> Implementations(Type openBase) => [.. provider.Assemblies
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => !type.IsAbstract && type.BaseType is { IsGenericType: true } parent
            && parent.GetGenericTypeDefinition() == openBase)
        .Select(type => new Implementation(type, type.BaseType!))];
}
