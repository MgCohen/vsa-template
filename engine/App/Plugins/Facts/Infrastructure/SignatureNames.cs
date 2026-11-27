namespace Facts.Infrastructure;

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Facts.Domain.Shared;

sealed class SignatureNames(MetadataNames names) : ISignatureTypeProvider<IReadOnlyList<TypeName>, object?> {
    static readonly IReadOnlyList<TypeName> None = [];

    public IReadOnlyList<TypeName> GetPrimitiveType(PrimitiveTypeCode typeCode) => [names.Primitive(typeCode)];

    public IReadOnlyList<TypeName> GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        [names.Of(handle)];

    public IReadOnlyList<TypeName> GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        [names.Of(handle)];

    public IReadOnlyList<TypeName> GetTypeFromSpecification(
        MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public IReadOnlyList<TypeName> GetSZArrayType(IReadOnlyList<TypeName> elementType) => elementType;

    public IReadOnlyList<TypeName> GetArrayType(IReadOnlyList<TypeName> elementType, ArrayShape shape) => elementType;

    public IReadOnlyList<TypeName> GetByReferenceType(IReadOnlyList<TypeName> elementType) => elementType;

    public IReadOnlyList<TypeName> GetPointerType(IReadOnlyList<TypeName> elementType) => elementType;

    public IReadOnlyList<TypeName> GetPinnedType(IReadOnlyList<TypeName> elementType) => elementType;

    public IReadOnlyList<TypeName> GetModifiedType(IReadOnlyList<TypeName> modifier, IReadOnlyList<TypeName> unmodifiedType, bool isRequired) =>
        unmodifiedType;

    public IReadOnlyList<TypeName> GetGenericInstantiation(
        IReadOnlyList<TypeName> genericType, ImmutableArray<IReadOnlyList<TypeName>> typeArguments) =>
        [.. genericType, .. typeArguments.SelectMany(argument => argument)];

    public IReadOnlyList<TypeName> GetGenericTypeParameter(object? genericContext, int index) => None;

    public IReadOnlyList<TypeName> GetGenericMethodParameter(object? genericContext, int index) => None;

    public IReadOnlyList<TypeName> GetFunctionPointerType(MethodSignature<IReadOnlyList<TypeName>> signature) => None;
}
