namespace BuiltApp.Infrastructure;

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;

sealed class SymbolReader : IDisposable {
    static readonly Guid TypeDefinitionDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
    const string EmbeddedAttribute = "Microsoft.CodeAnalysis.EmbeddedAttribute";
    const int OutputFolders = 2;
    const int GeneratorFolders = 2;
    static readonly char[] Separators = ['/', Path.DirectorySeparatorChar];

    readonly PEReader image;
    readonly MetadataReaderProvider? symbols;
    readonly MetadataReader metadata;
    readonly MetadataReader? pdbOrNull;
    readonly Lazy<IReadOnlyList<string>> sourceFiles;
    readonly Lazy<IReadOnlyList<string>> sourceFolders;
    readonly string? sourceRoot;
    readonly Dictionary<DocumentHandle, string> documentNames = [];
    readonly Dictionary<string, ILookup<string, string>> flatHints = [];

    public SymbolReader(string dll, string? sourceRoot = null) {
        image = new PEReader(File.OpenRead(dll));
        try {
            if (!image.HasMetadata) {
                throw new BadImageFormatException($"{dll} has no .NET metadata.");
            }
            metadata = image.GetMetadataReader();
        } catch {
            image.Dispose();
            throw;
        }
        symbols = PortablePdb(Path.ChangeExtension(dll, ".pdb")) ?? EmbeddedPdb(image);
        pdbOrNull = symbols?.GetMetadataReader();
        sourceFiles = new(() => [.. Documents().Where(file => !IsGenerated(file))]);
        sourceFolders = new(SourceFoldersAndParents);
        this.sourceRoot = sourceRoot;
    }

    public bool HasSymbols => pdbOrNull is not null;

    MetadataReader pdb => pdbOrNull!;

    public static string Resolve(string? sourceRoot, string document) =>
        sourceRoot is null || IsRooted(document)
            ? document
            : Path.GetFullPath(Path.Combine(sourceRoot, document.Replace('\\', Path.DirectorySeparatorChar)));

    public IReadOnlyList<string> References() =>
        [.. metadata.AssemblyReferences.Select(r => metadata.GetString(metadata.GetAssemblyReference(r).Name))];

    public IReadOnlyList<string> Documents() => HasSymbols ? [.. pdb.Documents.Select(DocumentName)] : [];

    public IEnumerable<TypeSource> TypeSources() {
        if (!HasSymbols) {
            yield break;
        }
        foreach (var handle in metadata.TypeDefinitions.Where(h => !IsEmbedded(h))) {
            if (Find(handle) is { } source) {
                yield return new TypeSource(FullName(handle), source);
            }
        }
    }

    public void Dispose() {
        symbols?.Dispose();
        image.Dispose();
    }

    static MetadataReaderProvider? EmbeddedPdb(PEReader image) {
        var entry = image.ReadDebugDirectory().FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
        return entry.DataSize == 0 ? null : image.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
    }

    static MetadataReaderProvider? PortablePdb(string file) {
        if (!File.Exists(file)) {
            return null;
        }
        var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(file));
        try {
            provider.GetMetadataReader();
            return provider;
        } catch (BadImageFormatException) {
            provider.Dispose();
            return null;
        }
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
        LoadedTypes.IsCompilerGenerated(metadata.GetString(metadata.GetTypeDefinition(handle).Name));

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

    static bool IsRooted(string document) =>
        Path.IsPathRooted(document)
        || document.StartsWith(@"\\", StringComparison.Ordinal)
        || document is [var drive, ':', '\\' or '/', ..] && char.IsAsciiLetter(drive);

    string DocumentName(DocumentHandle handle) {
        if (!documentNames.TryGetValue(handle, out var name)) {
            name = Resolve(sourceRoot, pdb.GetString(pdb.GetDocument(handle).Name));
            documentNames[handle] = name;
        }
        return name;
    }

    static bool IsGenerated(string file) => BuildOutput.Contains(file);

    Source? InProjectFolder(Source? source) {
        if (source is null || !IsGenerated(source.File)) {
            return source;
        }
        var segments = source.File.Split(Separators);
        var output = Array.FindIndex(segments, BuildOutput.IsFolder);
        var project = ArtifactsProjectFolder(segments[output + 1]) ?? string.Join("/", segments[..output]);
        var hint = HintPath(segments[(output + 1)..]);
        var path = hint.Length == 1 ? WithSourceFolders(project, hint[0]) : hint;
        return new Source($"{project}/{string.Join("/", path)}", 1);
    }

    string[] WithSourceFolders(string project, string hint) {
        if (project.Length == 0) {
            return [hint];
        }
        if (!flatHints.TryGetValue(project, out var hints)) {
            hints = sourceFiles.Value
                .Select(file => Path.GetRelativePath(project, file).Replace('\\', '/'))
                .ToLookup(FlatHint, StringComparer.Ordinal);
            flatHints[project] = hints;
        }
        if (hints[hint].Distinct(StringComparer.Ordinal).ToList() is not [var source]) {
            return [hint];
        }
        var folders = source.Split('/');
        return [.. folders[..^1], FlatHint(folders[^1])];
    }

    static string FlatHint(string relativePath) =>
        string.Concat(relativePath.Select(c => char.IsLetterOrDigit(c) ? c : '_')) + ".g.cs";

    string? ArtifactsProjectFolder(string projectName) =>
        sourceFolders.Value.FirstOrDefault(folder => File.Exists(Path.Combine(folder, projectName + ".csproj")))?.Replace('\\', '/');

    IReadOnlyList<string> SourceFoldersAndParents() =>
        [.. sourceFiles.Value
            .SelectMany(file => Parents(Path.GetDirectoryName(file)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(folder => folder.Length)];

    static IEnumerable<string> Parents(string? folder) {
        for (; !string.IsNullOrEmpty(folder); folder = Path.GetDirectoryName(folder)) {
            yield return folder;
        }
    }

    static string[] HintPath(string[] underOutput) {
        var generated = underOutput.Length > OutputFolders ? underOutput[OutputFolders..] : underOutput[^1..];
        return generated.Length > GeneratorFolders + 1 ? generated[GeneratorFolders..] : generated[^1..];
    }
}
