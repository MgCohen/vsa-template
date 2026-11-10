using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Structure;

public sealed class SourceMap
{
    static readonly Guid TypeDefinitionDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");

    readonly Dictionary<string, string> folderByType;

    SourceMap(Dictionary<string, string> folderByType) => this.folderByType = folderByType;

    public string? FolderOf(string typeFullName)
    {
        for (var name = typeFullName; ; name = name[..name.LastIndexOf('+')])
        {
            if (folderByType.TryGetValue(name, out var folder)) return folder;
            if (!name.Contains('+')) return null;
        }
    }

    public static SourceMap Read(string dllPath)
    {
        using var pe = new PEReader(File.OpenRead(dllPath));
        using var pdbProvider = OpenPdb(dllPath, pe);
        var pdb = pdbProvider.GetMetadataReader();
        var metadata = pe.GetMetadataReader();
        var filesByType = new Dictionary<TypeDefinitionHandle, List<string>>();

        foreach (var typeHandle in metadata.TypeDefinitions)
        {
            var files = new List<string>();
            foreach (var methodHandle in metadata.GetTypeDefinition(typeHandle).GetMethods())
            {
                var debug = pdb.GetMethodDebugInformation(methodHandle);
                if (debug.SequencePointsBlob.IsNil) continue;
                foreach (var point in debug.GetSequencePoints())
                    if (!point.IsHidden) files.Add(pdb.GetString(pdb.GetDocument(point.Document).Name));
            }
            filesByType[typeHandle] = files;
        }

        foreach (var handle in pdb.CustomDebugInformation)
        {
            var info = pdb.GetCustomDebugInformation(handle);
            if (info.Parent.Kind != HandleKind.TypeDefinition || pdb.GetGuid(info.Kind) != TypeDefinitionDocuments) continue;
            var reader = pdb.GetBlobReader(info.Value);
            var typeHandle = (TypeDefinitionHandle)info.Parent;
            while (reader.RemainingBytes > 0)
                filesByType[typeHandle].Add(pdb.GetString(pdb.GetDocument(MetadataTokens.DocumentHandle(reader.ReadCompressedInteger())).Name));
        }

        var allFiles = filesByType.Values.SelectMany(files => files).Distinct().ToList();
        var projectFolder = ProjectFolderOf(allFiles);
        var folderByType = new Dictionary<string, string>();
        foreach (var (typeHandle, files) in filesByType)
        {
            var file = files.FirstOrDefault(IsSource);
            if (file is null || projectFolder is null) continue;
            var relative = Path.GetRelativePath(projectFolder, Path.GetDirectoryName(file)!).Replace(Path.DirectorySeparatorChar, '/');
            folderByType[FullName(metadata, typeHandle)] = relative == "." ? "" : relative;
        }
        return new SourceMap(folderByType);
    }

    static bool IsSource(string file) =>
        !file.Replace(Path.DirectorySeparatorChar, '/').Contains("/obj/") && !file.Replace(Path.DirectorySeparatorChar, '/').Contains("/bin/");

    static string? ProjectFolderOf(List<string> files)
    {
        foreach (var file in files.Where(IsSource))
            for (var folder = Path.GetDirectoryName(file); folder is not null; folder = Path.GetDirectoryName(folder))
                if (Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.csproj").Any()) return folder;
        return null;
    }

    static string FullName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(type.Name);
        if (type.GetDeclaringType() is { IsNil: false } declaring) return FullName(metadata, declaring) + "+" + name;
        var ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    static MetadataReaderProvider OpenPdb(string dll, PEReader pe)
    {
        var embedded = pe.ReadDebugDirectory().FirstOrDefault(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
        if (embedded.DataSize > 0) return pe.ReadEmbeddedPortablePdbDebugDirectoryData(embedded);
        return MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(dll, ".pdb")));
    }
}
