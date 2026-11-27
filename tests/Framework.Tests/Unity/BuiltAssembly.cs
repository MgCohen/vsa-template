using Tests.Shared;

namespace Framework.Tests.Unity;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

public sealed record AssemblyReference(string Name, Version Version);

public sealed record BuiltAssembly(string Path, string TargetFramework, IReadOnlyList<AssemblyReference> References) {
    public static string Configuration { get; } =
        typeof(BuiltAssembly).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration.ToLowerInvariant();

    static readonly Dictionary<string, string> AssemblyOfProject = new(StringComparer.Ordinal) {
        ["Umpire.Vsa.Kernel"] = "Umpire.Vsa",
    };

    public static string BinFolder => Repo.Path("artifacts~", "bin");

    public static string AnalyzerEntry => "Umpire.Engine.Analyzer.dll";

    public static string AnalyzerFolder => Repo.Path("engine", "artifacts~", "bin", "Engine.Analyzer", $"{Configuration}-analyzer");

    public static BuiltAssembly OfSingleTarget(string project) =>
        Read(System.IO.Path.Combine(BinFolder, project, Configuration, $"{AssemblyOfProject.GetValueOrDefault(project, project)}.dll"));

    public static BuiltAssembly OfPortable(string project) => OfSingleTarget(project);

    public static BuiltAssembly OfAnalyzer(string file) => Read(System.IO.Path.Combine(AnalyzerFolder, file));

    public static IEnumerable<string> AnalyzerFiles() => Directory.GetFiles(AnalyzerFolder, "*.dll")
        .Select(System.IO.Path.GetFileName).OfType<string>()
        .Where(file => !file.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
        .Where(file => !file.StartsWith("System.", StringComparison.Ordinal) || file == "System.Reflection.MetadataLoadContext.dll")
        .Order(StringComparer.Ordinal);

    public static IEnumerable<string> UmpireAnalyzerFiles() =>
        AnalyzerFiles().Where(file => file.StartsWith("Umpire.", StringComparison.Ordinal));

    static BuiltAssembly Read(string path) {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var references = metadata.AssemblyReferences
            .Select(metadata.GetAssemblyReference)
            .Select(reference => new AssemblyReference(metadata.GetString(reference.Name), reference.Version))
            .ToList();

        return new BuiltAssembly(path, TargetFrameworkOf(metadata), references);
    }

    static string TargetFrameworkOf(MetadataReader metadata) {
        var attribute = metadata.GetAssemblyDefinition().GetCustomAttributes()
            .Select(metadata.GetCustomAttribute)
            .Single(a => AttributeTypeName(metadata, a) == "TargetFrameworkAttribute");
        var value = metadata.GetBlobReader(attribute.Value);
        value.ReadUInt16();

        return value.ReadSerializedString()!;
    }

    static string AttributeTypeName(MetadataReader metadata, CustomAttribute attribute) {
        if (attribute.Constructor.Kind != HandleKind.MemberReference) {
            return "";
        }
        var parent = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;

        return parent.Kind == HandleKind.TypeReference ? metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)parent).Name) : "";
    }
}
