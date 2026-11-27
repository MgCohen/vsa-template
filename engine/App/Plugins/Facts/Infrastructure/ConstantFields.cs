namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

sealed class ConstantFields {
    readonly HashSet<ConstantField> fields;

    ConstantFields(HashSet<ConstantField> fields) => this.fields = fields;

    public static ConstantFields Of(IReadOnlyList<string> dlls) => new([.. dlls.SelectMany(Read)]);

    public bool Contains(string type, string field) => fields.Contains(new ConstantField(type, field));

    static List<ConstantField> Read(string dll) {
        using var file = new PEReader(File.OpenRead(dll));
        if (!file.HasMetadata) {
            return [];
        }
        var metadata = file.GetMetadataReader();
        return [.. metadata.FieldDefinitions
            .Select(metadata.GetFieldDefinition)
            .Where(f => f.Attributes.HasFlag(FieldAttributes.Literal))
            .Select(f => new ConstantField(FullName(metadata, f.GetDeclaringType()), metadata.GetString(f.Name)))];
    }

    static string FullName(MetadataReader metadata, TypeDefinitionHandle handle) {
        var type = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(type.Name);
        if (type.GetDeclaringType() is { IsNil: false } outer) {
            return $"{FullName(metadata, outer)}+{name}";
        }
        var space = metadata.GetString(type.Namespace);
        return space.Length == 0 ? name : $"{space}.{name}";
    }

    sealed record ConstantField(string Type, string Field);
}
