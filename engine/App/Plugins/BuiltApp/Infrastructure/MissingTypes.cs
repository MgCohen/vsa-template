namespace BuiltApp.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BuiltApp.Domain.Shared;
using TypeName = BuiltApp.Domain.Shared.TypeName;

sealed class MissingTypes(MetadataLoadContext context) {
    const int QuotedType = 1;
    const int QuotedAssembly = 3;

    public UnloadedType Of(Type type, string source, Exception error) {
        var missing = Chain(type).Select(FindOrNull).FirstOrDefault(found => found is not null) ?? Quoted(error);
        return new UnloadedType(
            new TypeName(type.FullName!, type.Assembly.GetName().Name!), source, missing?.FullName,
            missing?.Assembly ?? AssemblyIn(error));
    }

    static IEnumerable<Type> Chain(Type type) {
        for (var current = type; current is not null; current = BaseOrNull(current)) {
            yield return current;
        }
    }

    static Type? BaseOrNull(Type type) {
        try {
            return type.BaseType;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return null;
        }
    }

    static TypeName? Quoted(Exception error) {
        var parts = error.Message.Split('\'');
        return error is TypeLoadException && parts.Length > QuotedAssembly
            ? new TypeName(parts[QuotedType], parts[QuotedAssembly].Split(',')[0])
            : null;
    }

    static string AssemblyIn(Exception error) {
        var parts = error.Message.Split('\'');
        return parts.Length > QuotedType ? parts[QuotedType].Split(',')[0] : error.Message;
    }

    TypeName? FindOrNull(Type type) {
        try {
            return Find(type);
        } catch (Exception e) when (e is BadImageFormatException or IOException) {
            return null;
        }
    }

    TypeName? Find(Type type) {
        using var image = new PEReader(File.OpenRead(type.Assembly.Location));
        var metadata = image.GetMetadataReader();
        var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.Handle(type.MetadataToken));
        var bases = definition.GetInterfaceImplementations()
            .Select(handle => metadata.GetInterfaceImplementation(handle).Interface)
            .Prepend(definition.BaseType);
        return bases.SelectMany(handle => References(metadata, handle))
            .Select(handle => Name(metadata, handle))
            .FirstOrDefault(name => name is not null && IsMissing(name));
    }

    static List<TypeReferenceHandle> References(MetadataReader metadata, EntityHandle handle) {
        if (handle.Kind == HandleKind.TypeReference) {
            return [(TypeReferenceHandle)handle];
        }
        if (handle.Kind != HandleKind.TypeSpecification) {
            return [];
        }
        var blob = metadata.GetBlobReader(metadata.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        var found = new List<TypeReferenceHandle>();
        Read(ref blob, found);
        return found;
    }

    static void Read(ref BlobReader blob, List<TypeReferenceHandle> found) {
        switch (blob.ReadSignatureTypeCode()) {
            case SignatureTypeCode.TypeHandle:
                Add(blob.ReadTypeHandle(), found);
                break;
            case SignatureTypeCode.GenericTypeInstance:
                blob.ReadSignatureTypeCode();
                Add(blob.ReadTypeHandle(), found);
                for (var count = blob.ReadCompressedInteger(); count > 0; count--) {
                    Read(ref blob, found);
                }
                break;
            case SignatureTypeCode.SZArray:
                Read(ref blob, found);
                break;
            case SignatureTypeCode.GenericTypeParameter:
                blob.ReadCompressedInteger();
                break;
            default:
                break;
        }
    }

    static void Add(EntityHandle handle, List<TypeReferenceHandle> found) {
        if (handle.Kind == HandleKind.TypeReference) {
            found.Add((TypeReferenceHandle)handle);
        }
    }

    static TypeName? Name(MetadataReader metadata, TypeReferenceHandle handle) {
        var type = metadata.GetTypeReference(handle);
        if (type.ResolutionScope.Kind != HandleKind.AssemblyReference) {
            return null;
        }
        var assembly = metadata.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);
        return new TypeName(
            $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}", metadata.GetString(assembly.Name));
    }

    bool IsMissing(TypeName name) {
        try {
            context.LoadFromAssemblyName(name.Assembly);
            return false;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException) {
            return true;
        }
    }
}
