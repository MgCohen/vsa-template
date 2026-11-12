using MetadataReaderProvider = System.Reflection.Metadata.MetadataReaderProvider;
using System.Reflection.Metadata.Ecma335;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace VSA.Architecture.Engine;

public static class SourceFiles {
    static readonly Guid TypeDefinitionDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
    static readonly string[] GeneratedFolders = ["obj", "bin"];
    static readonly char[] Separators = ['/', Path.DirectorySeparatorChar];
    static readonly Dictionary<string, Dictionary<int, string>> Documents = [];

    public static Dictionary<string, Source> Read(IEnumerable<string> dlls) {
        var result = new Dictionary<string, Source>();
        foreach (var dll in dlls.Where(HasSymbols)) {
            using var module = ModuleDefinition.ReadModule(dll, new ReaderParameters { ReadSymbols = true });
            foreach (var type in module.Types.SelectMany(Walk)) {
                if (Find(type) is { } source) {
                    result[type.FullName.Replace('/', '+')] = source;
                }
            }
        }
        return result;
    }

    public static bool HasSymbols(string dll) => File.Exists(Path.ChangeExtension(dll, ".pdb"));

    static IEnumerable<TypeDefinition> Walk(TypeDefinition type) => new[] { type }.Concat(type.NestedTypes.SelectMany(Walk));

    static Source? Find(TypeDefinition type) {
        var candidates = new[] { Own(type) }
            .Concat(type.NestedTypes.Where(n => n.Name.StartsWith('<')).Select(Own))
            .OfType<Source>()
            .OrderBy(s => IsGenerated(s.File))
            .ToList();
        return InProjectFolder(candidates.FirstOrDefault() ?? (type.DeclaringType is null ? null : Find(type.DeclaringType)));
    }

    static bool IsGenerated(string file) =>
        file.Split(Separators).Any(segment => GeneratedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));

    static Source? InProjectFolder(Source? source) {
        if (source is null || !IsGenerated(source.File)) {
            return source;
        }
        var segments = source.File.Split(Separators);
        var project = string.Join("/", segments.TakeWhile(segment => !GeneratedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase)));
        return new Source($"{project}/{segments[^1]}", 1);
    }

    static Source? Own(TypeDefinition type) {
        var point = type.Methods.Where(m => m.HasBody)
            .SelectMany(m => m.DebugInformation.SequencePoints)
            .Where(p => !p.IsHidden)
            .OrderBy(p => IsGenerated(p.Document.Url))
            .ThenBy(p => p.StartLine)
            .FirstOrDefault();
        if (point is not null) {
            return new Source(point.Document.Url, point.StartLine);
        }
        var info = type.CustomDebugInformations.OfType<BinaryCustomDebugInformation>().FirstOrDefault(i => i.Identifier == TypeDefinitionDocuments);
        if (info is null) {
            return null;
        }
        var row = ReadCompressed(info.Data);
        return DocumentsOf(type.Module).TryGetValue(row, out var file) ? new Source(file, 1) : null;
    }

    static Dictionary<int, string> DocumentsOf(ModuleDefinition module) {
        var dll = module.FileName;
        lock (Documents) {
            if (Documents.TryGetValue(dll, out var cached)) {
                return cached;
            }
            using var stream = File.OpenRead(Path.ChangeExtension(dll, ".pdb"));
            using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
            var pdb = provider.GetMetadataReader();
            return Documents[dll] = pdb.Documents.ToDictionary(
                h => MetadataTokens.GetRowNumber(h),
                h => pdb.GetString(pdb.GetDocument(h).Name));
        }
    }

    static int ReadCompressed(byte[] data) {
        var first = data[0];
        if ((first & 0x80) == 0) {
            return first;
        }
        if ((first & 0xC0) == 0x80) {
            return ((first & 0x3F) << 8) | data[1];
        }
        return ((first & 0x1F) << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
    }
}
