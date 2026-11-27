namespace LoadApp.Slices.LoadApp;

using Facts.Domain.Shared;
using Umpire.Messages.Contracts;

static class DefinitionSearch {
    const int ShownTypes = 5;

    public static Result<TypeName> Find(
        BlockEntry entry, IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions,
        IReadOnlyList<BlockEntry>? inner = null, bool hasDefinitionSource = false) {
        var unloaded = app.SelectMany(d => d.Unloaded).ToList();
        if (entry.Form == EntryForm.Definition && unloaded.Count > 0 && !HoldsKernel(entry, app, definitions)) {
            return Missing("The block has no app class that loads", unloaded);
        }
        if (entry.Form != EntryForm.App && !entry.HasApp) {
            return entry.Type!;
        }
        var classes = AppClass.Leaves([.. app.SelectMany(d => d.AppClasses)]);
        if (classes.Count == 0 && unloaded.Count > 0) {
            return Missing("The app has no app class that loads", unloaded);
        }
        if (AppClassError(classes) is { } error) {
            return error;
        }
        if (unloaded.Any(u => Same(u.Folder, entry.Folder))) {
            return Missing("The app has no definition that loads", [.. unloaded.Where(u => Same(u.Folder, entry.Folder))]);
        }
        if (hasDefinitionSource && !definitions.Any(d => IsIn(d, entry.Folder))) {
            return NoDefinitionLoaded(entry.Folder);
        }
        var found = Select(entry, classes[0], definitions, [.. unloaded.Where(u => u.Folder is not null)]);
        return found.IsSuccess && Ignored(entry, classes[0], found.Value, Own(app, definitions), inner ?? []) is { } ignored
            ? ignored
            : found;
    }

    static Result<TypeName> Select(
        BlockEntry entry, AppClass app, IReadOnlyList<DefinitionType> definitions, List<UnloadedType> unloaded) {
        if (!entry.HasApp) {
            return OfApp(app, entry.Folder, definitions, unloaded);
        }
        return OtherKernel(app, entry.Type!, definitions) is { } other ? other : entry.Type!;
    }

    static List<DefinitionType> Own(IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions) =>
        [.. definitions.Where(d => app.Any(dll => dll.Name == d.Type.Assembly))];

    static Error? Ignored(
        BlockEntry entry, AppClass app, TypeName selected, List<DefinitionType> definitions,
        IReadOnlyList<BlockEntry> inner) {
        var used = definitions.Where(d => d.Type == selected || IsOfInner(d, app, inner)).ToList();
        var ignored = definitions
            .Where(d => d.Source is { } source && IsUnder(source, entry.Folder) && !used.Contains(d)
                && !used.Any(u => u.Bases.Contains(d.Type.FullName)))
            .Select(d => d.Type.FullName).Order(StringComparer.Ordinal).ToList();
        return ignored.Count == 0
            ? null
            : new Error(LoadAppCodes.IgnoredDefinition,
                $"The check of the app uses the definition {selected.FullName}, and it ignores {string.Join(", ", ignored)}. "
                + $"Put the definition of the app in {DefinitionType.FolderName}/, or make it a base of the used definition. "
                + "The definition of a pack must name a kernel of the pack, and the app class names that kernel.");
    }

    static bool IsOfInner(DefinitionType definition, AppClass app, IReadOnlyList<BlockEntry> inner) => inner.Any(e => e.Form switch {
        EntryForm.App => e.HasProject && IsUnder(definition.Source, e.Folder),
        EntryForm.Definition => e.Definition == definition.Type && (e.HasProject || definition.Kernel != app.Kernels[0]),
        _ => e.Definition == definition.Type || IsUnder(definition.Source, e.Folder),
    });

    static Result<TypeName> OfApp(
        AppClass app, string folder, IReadOnlyList<DefinitionType> definitions, List<UnloadedType> unloaded) {
        var own = DefinitionType.Leaves([.. definitions.Where(d => IsIn(d, folder))]);
        if (own.Count > 1) {
            return Many($"The folder {DefinitionType.FolderName} of the block {folder} has more than one definition", own);
        }
        if (own.Count == 1) {
            return OtherKernel(app, own[0].Type, definitions) is { } other ? other : own[0].Type;
        }
        var found = OfKernels(app, definitions);
        if (unloaded.Count > 0 && (found.Count == 0 || found[0].Kernel != app.Kernels[0])) {
            return Missing($"No definition of the kernel {app.Kernels[0].FullName} loads", unloaded);
        }
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

    static bool HoldsKernel(BlockEntry entry, IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions) =>
        definitions.FirstOrDefault(d => d.Type == entry.Definition)?.Kernel is { } kernel
        && app.Any(dll => dll.Name == kernel.Assembly);

    static Error Missing(string problem, List<UnloadedType> unloaded) =>
        new(LoadAppCodes.MissingAssembly,
            $"{problem}. {string.Join(" ", unloaded.OrderBy(u => u.Source.Length).Take(ShownTypes))} "
            + "Give the folder of the assembly with --references. If the class is a definition, the definition project must "
            + "reference the assembly and copy it to its output.");

    static Error NoDefinitionLoaded(string folder) =>
        new(LoadAppCodes.EmptyDefinitionFolder,
            $"The folder {DefinitionType.FolderName} of the block {folder} has source files, and no architecture with a PDB "
            + "loads from it. Build the definition project, and give its DLL in the bin or in the folder umpire-definitions of the bin.");

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
        Same(definition.Folder, folder);

    static bool IsUnder(string? path, string folder) =>
        path is not null && path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    static bool Same(string? a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    static string Names(IEnumerable<AppClass> classes) =>
        string.Join(", ", classes.Select(c => c.Type.FullName).Order(StringComparer.Ordinal));
}
