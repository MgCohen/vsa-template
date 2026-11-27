namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata;
using Facts.Domain.Shared;

sealed class MetadataNames {
    readonly MetadataReader metadata;
    readonly MetadataLoadContext context;
    readonly string assembly;
    readonly Dictionary<EntityHandle, TypeName> known = [];

    public MetadataNames(MetadataReader metadata, MetadataLoadContext context, string assembly) {
        this.metadata = metadata;
        this.context = context;
        this.assembly = assembly;
        Signatures = new SignatureNames(this);
    }

    public SignatureNames Signatures { get; }

    public TypeName Primitive(PrimitiveTypeCode code) => new("System." + code, context.CoreAssembly?.GetName().Name ?? "");

    public TypeName Of(TypeDefinitionHandle handle) {
        if (!known.TryGetValue(handle, out var name)) {
            name = new TypeName(FullName(handle), assembly);
            known[handle] = name;
        }
        return name;
    }

    public TypeName Of(TypeReferenceHandle handle) {
        if (!known.TryGetValue(handle, out var name)) {
            var fullName = FullName(handle);
            name = new TypeName(fullName, Resolved(fullName, ScopeOf(handle)));
            known[handle] = name;
        }
        return name;
    }

    public IReadOnlyList<TypeName> Of(EntityHandle handle) => handle.Kind switch {
        HandleKind.TypeDefinition => [Of((TypeDefinitionHandle)handle)],
        HandleKind.TypeReference => [Of((TypeReferenceHandle)handle)],
        HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(Signatures, null),
        _ => [],
    };

    public TypeName? Head(EntityHandle handle) {
        var all = Of(handle);
        return all.Count > 0 ? all[0] : null;
    }

    public string FullName(TypeDefinitionHandle handle) {
        var type = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(type.Name);
        var declaring = type.GetDeclaringType();
        if (!declaring.IsNil) {
            return $"{FullName(declaring)}+{name}";
        }
        var space = metadata.GetString(type.Namespace);
        return space.Length == 0 ? name : $"{space}.{name}";
    }

    string FullName(TypeReferenceHandle handle) {
        var type = metadata.GetTypeReference(handle);
        var name = metadata.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference) {
            return $"{FullName((TypeReferenceHandle)type.ResolutionScope)}+{name}";
        }
        var space = metadata.GetString(type.Namespace);
        return space.Length == 0 ? name : $"{space}.{name}";
    }

    string ScopeOf(TypeReferenceHandle handle) {
        var scope = metadata.GetTypeReference(handle).ResolutionScope;
        return scope.Kind switch {
            HandleKind.AssemblyReference => metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
            HandleKind.TypeReference => ScopeOf((TypeReferenceHandle)scope),
            _ => assembly,
        };
    }

    string Resolved(string fullName, string scope) {
        try {
            return context.LoadFromAssemblyName(scope).GetType(fullName)?.Assembly.GetName().Name ?? scope;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException) {
            return scope;
        }
    }
}
