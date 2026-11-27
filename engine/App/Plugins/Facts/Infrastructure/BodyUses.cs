namespace Facts.Infrastructure;

using System.Reflection.Emit;
using System.Reflection.Metadata;
using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;

sealed class BodyUses(SymbolReader symbols, MetadataNames names) {
    MetadataReader Metadata => symbols.Metadata;

    public IEnumerable<BodyUse> Of(MethodDefinitionHandle method) {
        if (symbols.BodyOf(method) is not { } body) {
            return [];
        }
        var found = new List<BodyUse>();
        foreach (var token in IlCode.Tokens(body)) {
            found.AddRange(Used(token));
        }
        if (!body.LocalSignature.IsNil) {
            var locals = Metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(names.Signatures, null);
            found.AddRange(locals.SelectMany(local => local).Select(name => new BodyUse(name, DllUseKind.Inferred, 0, null)));
        }
        foreach (var region in body.ExceptionRegions.Where(r => !r.CatchType.IsNil)) {
            found.AddRange(names.Of(region.CatchType).Select(name => new BodyUse(name, DllUseKind.Type, region.HandlerOffset, null)));
        }
        return found;
    }

    IEnumerable<BodyUse> Used(IlToken token) {
        var handle = token.Handle;
        switch (handle.Kind) {
            case HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification:
                return Named(names.Of(handle), token.Code == OpCodes.Ldtoken ? DllUseKind.TypeOf : DllUseKind.Type, token.Offset, null);
            case HandleKind.MethodDefinition:
                var method = Metadata.GetMethodDefinition((MethodDefinitionHandle)handle);
                return Named([names.Of(method.GetDeclaringType())], CallKind(token), token.Offset, Metadata.GetString(method.Name));
            case HandleKind.FieldDefinition:
                var field = Metadata.GetFieldDefinition((FieldDefinitionHandle)handle);
                return Named([names.Of(field.GetDeclaringType())], DllUseKind.Member, token.Offset, Metadata.GetString(field.Name));
            case HandleKind.MemberReference:
                return OfReference((MemberReferenceHandle)handle, token);
            case HandleKind.MethodSpecification:
                var specification = Metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                var arguments = specification.DecodeSignature(names.Signatures, null).SelectMany(a => a)
                    .Select(name => new BodyUse(name, DllUseKind.TypeArgument, token.Offset, null));
                return Used(token with { Handle = specification.Method }).Concat(arguments);
            default:
                return [];
        }
    }

    IEnumerable<BodyUse> OfReference(MemberReferenceHandle handle, IlToken token) {
        var reference = Metadata.GetMemberReference(handle);
        var parent = reference.Parent.Kind == HandleKind.MethodDefinition
            ? [names.Of(Metadata.GetMethodDefinition((MethodDefinitionHandle)reference.Parent).GetDeclaringType())]
            : names.Of(reference.Parent);
        var kind = reference.GetKind() == MemberReferenceKind.Field ? DllUseKind.Member : CallKind(token);
        return Named(parent, kind, token.Offset, Metadata.GetString(reference.Name));
    }

    static DllUseKind CallKind(IlToken token) => token.Code == OpCodes.Newobj ? DllUseKind.Create : DllUseKind.Call;

    static IEnumerable<BodyUse> Named(IReadOnlyList<TypeName> found, DllUseKind kind, int offset, string? member) =>
        found.Select((name, index) => index == 0
            ? new BodyUse(name, kind, offset, member)
            : new BodyUse(name, DllUseKind.TypeArgument, offset, null));
}
