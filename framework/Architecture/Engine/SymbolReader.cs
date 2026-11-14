using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace VSA.Architecture.Engine;

sealed class SymbolReader : IDisposable {
    static readonly Guid TypeDefinitionDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
    static readonly string[] GeneratedFolders = ["obj", "bin"];
    const string EmbeddedAttribute = "Microsoft.CodeAnalysis.EmbeddedAttribute";
    const int OutputFolders = 2;
    const int GeneratorFolders = 2;
    static readonly char[] Separators = ['/', Path.DirectorySeparatorChar];

    readonly PEReader image;
    readonly MetadataReaderProvider symbols;
    readonly MetadataReader metadata;
    readonly MetadataReader pdb;

    public SymbolReader(string dll) {
        image = new PEReader(File.OpenRead(dll));
        symbols = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(dll, ".pdb")));
        metadata = image.GetMetadataReader();
        pdb = symbols.GetMetadataReader();
    }

    public IReadOnlyList<string> Documents() => [.. pdb.Documents.Select(DocumentName)];

    public IEnumerable<TypeSource> TypeSources() {
        foreach (var handle in metadata.TypeDefinitions.Where(h => !IsEmbedded(h))) {
            if (Find(handle) is { } source) {
                yield return new TypeSource(FullName(handle), source);
            }
        }
    }

    public void Dispose() {
        symbols.Dispose();
        image.Dispose();
    }

    bool IsEmbedded(TypeDefinitionHandle handle) =>
        metadata.GetTypeDefinition(handle).GetCustomAttributes()
            .Select(a => AttributeTypeName(metadata.GetCustomAttribute(a)))
            .Any(name => name == EmbeddedAttribute);

    string? AttributeTypeName(CustomAttribute attribute) => attribute.Constructor.Kind switch {
        HandleKind.MethodDefinition => FullName(metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()),
        HandleKind.MemberReference => ReferenceName(metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
        _ => null,
    };

    string? ReferenceName(EntityHandle parent) {
        if (parent.Kind != HandleKind.TypeReference) {
            return null;
        }
        var type = metadata.GetTypeReference((TypeReferenceHandle)parent);
        return $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}";
    }

    string FullName(TypeDefinitionHandle handle) {
        var type = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(type.Name);
        var declaring = type.GetDeclaringType();
        if (!declaring.IsNil) {
            return $"{FullName(declaring)}+{name}";
        }
        var space = metadata.GetString(type.Namespace);
        return space.Length == 0 ? name : $"{space}.{name}";
    }

    Source? Find(TypeDefinitionHandle handle) {
        var type = metadata.GetTypeDefinition(handle);
        var candidates = new[] { Own(handle) }
            .Concat(type.GetNestedTypes().Where(IsCompilerGenerated).Select(Own))
            .OfType<Source>()
            .OrderBy(s => IsGenerated(s.File))
            .ToList();
        var declaring = type.GetDeclaringType();
        return InProjectFolder(candidates.FirstOrDefault() ?? (declaring.IsNil ? null : Find(declaring)));
    }

    bool IsCompilerGenerated(TypeDefinitionHandle handle) =>
        metadata.GetString(metadata.GetTypeDefinition(handle).Name).StartsWith('<');

    Source? Own(TypeDefinitionHandle handle) {
        var points = metadata.GetTypeDefinition(handle).GetMethods()
            .SelectMany(SequencePoints)
            .Where(p => !p.IsHidden)
            .OrderBy(p => IsGenerated(DocumentName(p.Document)))
            .ThenBy(p => p.StartLine)
            .Take(1)
            .ToList();
        if (points.Count == 1) {
            return new Source(DocumentName(points[0].Document), points[0].StartLine, points[0].StartColumn);
        }
        return TypeDocument(handle) is { } file ? new Source(file, 1) : null;
    }

    IEnumerable<SequencePoint> SequencePoints(MethodDefinitionHandle method) {
        var info = pdb.GetMethodDebugInformation(method.ToDebugInformationHandle());
        return info.SequencePointsBlob.IsNil ? [] : info.GetSequencePoints();
    }

    string? TypeDocument(TypeDefinitionHandle handle) {
        foreach (var entry in pdb.GetCustomDebugInformation(handle)) {
            var info = pdb.GetCustomDebugInformation(entry);
            if (pdb.GetGuid(info.Kind) != TypeDefinitionDocuments) {
                continue;
            }
            var row = pdb.GetBlobReader(info.Value).ReadCompressedInteger();
            return row >= 1 && row <= pdb.Documents.Count ? DocumentName(MetadataTokens.DocumentHandle(row)) : null;
        }
        return null;
    }

    string DocumentName(DocumentHandle handle) => pdb.GetString(pdb.GetDocument(handle).Name);

    static bool IsGenerated(string file) =>
        file.Split(Separators).Any(segment => GeneratedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));

    static Source? InProjectFolder(Source? source) {
        if (source is null || !IsGenerated(source.File)) {
            return source;
        }
        var segments = source.File.Split(Separators);
        var output = Array.FindIndex(segments, segment => GeneratedFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));
        var project = string.Join("/", segments[..output]);
        return new Source($"{project}/{string.Join("/", HintPath(segments[(output + 1)..]))}", 1);
    }

    static string[] HintPath(string[] underOutput) {
        var generated = underOutput.Length > OutputFolders ? underOutput[OutputFolders..] : underOutput[^1..];
        return generated.Length > GeneratorFolders + 1 ? generated[GeneratorFolders..] : generated[^1..];
    }
}
