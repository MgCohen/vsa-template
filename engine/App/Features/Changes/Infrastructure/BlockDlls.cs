namespace Changes.Infrastructure;

using BuiltApp.Domain.Shared;

public static class BlockDlls {
    public static IReadOnlyList<string> Of(BlockEntry block, BinIndex bin) {
        var own = bin.AppDlls(block.Folder, []);
        var referenced = bin.ReferencedDlls(own, []);
        var definition = bin.Dlls.Where(dll => IsAssemblyOf(dll, block.Type) || (IsApp(block) && HasKernelDefinition(dll)));
        var shared = bin.Dlls.Where(dll => dll.IsShared);
        var dlls = own.Concat(referenced).Concat(definition).Concat(shared).Select(dll => dll.Path);
        return [.. dlls.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    static bool IsApp(BlockEntry block) => block.Form == EntryForm.App || block.HasApp;

    static bool HasKernelDefinition(BinDll dll) => dll.Definitions.Any(d => d.Kernel is not null);

    static bool IsAssemblyOf(BinDll dll, TypeName? type) =>
        type is not null && string.Equals(dll.Name, type.Assembly, StringComparison.OrdinalIgnoreCase);
}
