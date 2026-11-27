namespace Facts.Infrastructure;

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

public sealed record NewerDll(string Dll, Version Runtime);

public static class TargetRuntime {
    const string Attribute = "TargetFrameworkAttribute";
    const string CoreApp = ".NETCoreApp,Version=v";

    public static NewerDll? NewerThanEngine(string bin) =>
        NewerThan(bin, Environment.Version);

    public static NewerDll? NewerThan(string bin, Version engine) {
        var folders = new[] { bin, BinReader.DefinitionsFolder(bin) }.Where(Directory.Exists);
        return folders.SelectMany(folder => Directory.GetFiles(folder, "*.dll")).Order(StringComparer.OrdinalIgnoreCase)
            .Select(dll => Of(dll) is { } runtime ? new NewerDll(dll, runtime) : null)
            .FirstOrDefault(newer => newer is not null && newer.Runtime.Major > engine.Major);
    }

    public static Version? Of(string dll) {
        try {
            using var stream = File.OpenRead(dll);
            using var pe = new PEReader(stream);
            return pe.HasMetadata ? Of(pe.GetMetadataReader()) : null;
        } catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException) {
            return null;
        }
    }

    static Version? Of(MetadataReader reader) {
        if (!reader.IsAssembly) {
            return null;
        }
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes()) {
            var attribute = reader.GetCustomAttribute(handle);
            if (TypeName(reader, attribute) != Attribute) {
                continue;
            }
            var value = reader.GetBlobReader(attribute.Value);
            value.ReadUInt16();
            var name = value.ReadSerializedString();
            return name is not null && name.StartsWith(CoreApp, StringComparison.Ordinal)
                && Version.TryParse(name[CoreApp.Length..], out var version) ? version : null;
        }
        return null;
    }

    static string? TypeName(MetadataReader reader, CustomAttribute attribute) {
        if (attribute.Constructor.Kind != HandleKind.MemberReference) {
            return null;
        }
        var member = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        return member.Parent.Kind == HandleKind.TypeReference
            ? reader.GetString(reader.GetTypeReference((TypeReferenceHandle)member.Parent).Name)
            : null;
    }
}
