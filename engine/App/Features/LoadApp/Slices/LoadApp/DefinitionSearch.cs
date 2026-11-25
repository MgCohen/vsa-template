namespace LoadApp.Slices.LoadApp;

using BuiltApp.Domain.Shared;
using Umpire.Messages.Contracts;

static class DefinitionSearch {
    public static Result<TypeName> Find(
        BlockEntry entry, IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions) {
        if (entry.Form != EntryForm.App && !entry.HasApp) {
            return entry.Type!;
        }
        var classes = AppClass.Leaves([.. app.SelectMany(d => d.AppClasses)]);
        if (AppClassError(classes) is { } error) {
            return error;
        }
        if (!entry.HasApp) {
            return OfApp(classes[0], entry.Folder, definitions);
        }
        return OtherKernel(classes[0], entry.Type!, definitions) is { } other ? other : entry.Type!;
    }

    static Result<TypeName> OfApp(AppClass app, string folder, IReadOnlyList<DefinitionType> definitions) {
        var own = DefinitionType.Leaves([.. definitions.Where(d => IsIn(d, folder))]);
        if (own.Count > 1) {
            return Many($"The folder {DefinitionType.FolderName} of the block {folder} has more than one definition", own);
        }
        if (own.Count == 1) {
            return OtherKernel(app, own[0].Type, definitions) is { } other ? other : own[0].Type;
        }
        var found = OfKernels(app, definitions);
        return found.Count switch {
            0 => new Error(LoadAppCodes.NoDefinition,
                $"No definition names the kernel {app.Kernels[0].FullName} of the app class {app.Type.FullName}. "
                + "Add a class that inherits Architecture<TKernel> for this kernel, for example in the pack of the kernel."),
            1 => found[0].Type,
            _ => Many($"The kernel {found[0].Kernel!.FullName} has more than one definition", found),
        };
    }

    static Error? OtherKernel(AppClass app, TypeName named, IReadOnlyList<DefinitionType> definitions) {
        if (definitions.FirstOrDefault(d => d.Type == named) is not { } definition) {
            return null;
        }
        if (definition.Kernel is not { } kernel) {
            return new Error(LoadAppCodes.OtherKernel,
                $"The definition {named.FullName} names no kernel, and the app class {app.Type.FullName} names "
                + $"{app.Kernels[0].FullName}. Inherit Architecture<TKernel> or the architecture of the pack.");
        }
        var expected = OfKernels(app, definitions) is [var first, ..] ? first.Kernel : null;
        return kernel == expected || kernel == app.Kernels[0]
            ? null
            : new Error(LoadAppCodes.OtherKernel,
                $"The app class {app.Type.FullName} names the kernel {app.Kernels[0].FullName}, and the definition "
                + $"{named.FullName} names the kernel {kernel.FullName}. Name one kernel in both.");
    }

    static IReadOnlyList<DefinitionType> OfKernels(AppClass app, IReadOnlyList<DefinitionType> definitions) => app.KernelChain
        .Select(kernel => DefinitionType.Roots([.. definitions.Where(d => d.Kernel == kernel)]))
        .FirstOrDefault(found => found.Count > 0) ?? [];

    static Error Many(string problem, IReadOnlyList<DefinitionType> found) =>
        new(LoadAppCodes.ManyDefinitions,
            $"{problem}: {string.Join(", ", found.Select(d => d.Type.FullName).Order(StringComparer.Ordinal))}. "
            + "Keep one, or make one inherit the other.");

    static Error? AppClassError(IReadOnlyList<AppClass> classes) =>
        classes.Where(c => c.HasManyKernels).ToList() is [_, ..] many
            ? new Error(LoadAppCodes.ManyKernels,
                $"The app class {Names(many)} implements IAppEntry<TKernel> for more than one kernel: "
                + $"{string.Join(", ", many.SelectMany(c => c.Kernels).Select(k => k.FullName))}. Implement it for one kernel only.")
            : CountError(classes);

    static Error? CountError(IReadOnlyList<AppClass> classes) =>
        classes.Count switch {
            0 => new Error(LoadAppCodes.NoAppClass, "The app has no app class. "
                + "Add one class that inherits App<TKernel, THost>, for example App<VsaKernel, WebHost>, "
                + "or a class that implements IAppEntry<TKernel>."),
            > 1 => new Error(LoadAppCodes.ManyAppClasses,
                $"The app has more than one app class: {Names(classes)}. Keep one, or make one inherit the other."),
            _ => null,
        };

    static bool IsIn(DefinitionType definition, string folder) =>
        string.Equals(definition.Folder, folder, StringComparison.OrdinalIgnoreCase);

    static string Names(IEnumerable<AppClass> classes) =>
        string.Join(", ", classes.Select(c => c.Type.FullName).Order(StringComparer.Ordinal));
}
