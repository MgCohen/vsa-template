using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

namespace VSA.Architecture.Data;

sealed class TypeResolver {
    const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    readonly Dictionary<string, Type> types = new(StringComparer.Ordinal);

    public TypeResolver(DefinitionFile file, MissingTypes missing) {
        HashSet<string> concepts = [.. ConceptNames(file)];
        HashSet<string> names = [.. concepts, .. file.Parts.SelectMany(Walk).SelectMany(p => p.Types)];
        foreach (var name in names) {
            if (Loaded(name) is { } type) {
                types[name] = type;
            }
        }
        var absent = names.Where(n => !types.ContainsKey(n)).ToList();
        if (missing == MissingTypes.StandIn) {
            foreach (var assembly in absent.GroupBy(n => TypeName.Parse(n).Assembly)) {
                Build(assembly.Key, [.. assembly], concepts);
            }
        }
    }

    public Type Of(string name) => types.TryGetValue(name, out var type)
        ? type
        : throw new JsonException(
            $"The definition file names the type {name}, and no loaded assembly has it. "
            + "Reference the assembly of the type, or correct the name in the file.");

    public static IEnumerable<string> ConceptNames(DefinitionFile file) =>
        file.Parts.SelectMany(Walk).Select(p => p.Concept)
            .Concat(file.Rules.SelectMany(r => r.From.Concat(r.To)).SelectMany(t => t.Filters.Select(f => f.Concept).Append(t.Concept)));

    static IEnumerable<PartNode> Walk(PartNode part) => part.Children.SelectMany(Walk).Prepend(part);

    static Type? Loaded(string name) {
        var type = Type.GetType(name);
        if (type is not null) {
            return type;
        }
        var fullName = TypeName.Parse(name).FullName;
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.GetType(fullName))
            .FirstOrDefault(t => t is not null);
    }

    void Build(string assembly, List<string> names, HashSet<string> concepts) {
        var assemblyName = assembly.Length > 0 ? assembly : "StandIns";
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            .DefineDynamicModule(assemblyName);
        HashSet<string> all = [.. names.SelectMany(WithOuters)];
        var ordered = all.OrderBy(n => TypeName.Parse(n).Depth).ToList();
        Dictionary<string, TypeBuilder> builders = [];
        foreach (var name in ordered) {
            builders[name] = Define(module, builders, TypeName.Parse(name), concepts.Contains(name));
        }
        foreach (var name in ordered) {
            types[name] = builders[name].CreateType()!;
        }
    }

    static IEnumerable<string> WithOuters(string name) {
        for (string? at = name; at is not null; at = TypeName.Parse(at).Outer) {
            yield return at;
        }
    }

    static TypeBuilder Define(ModuleBuilder module, Dictionary<string, TypeBuilder> builders, TypeName name, bool concept) {
        var kind = concept ? TypeAttributes.Sealed : TypeAttributes.Abstract;
        var parent = concept ? typeof(Concept) : typeof(object);
        var builder = name.Outer is null
            ? module.DefineType(name.FullName, TypeAttributes.Public | TypeAttributes.Class | kind, parent)
            : builders[name.Outer].DefineNestedType(name.Simple, TypeAttributes.NestedPublic | TypeAttributes.Class | kind, parent);
        if (name.Arity > 0) {
            builder.DefineGenericParameters([.. Enumerable.Range(0, name.Arity).Select(i => $"T{i}")]);
        }
        if (concept) {
            ConceptMembers(builder);
        }
        return builder;
    }

    static void ConceptMembers(TypeBuilder builder) {
        var constructor = builder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard, Type.EmptyTypes);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(Concept).GetConstructor(Members, null, Type.EmptyTypes, null)!);
        il.Emit(OpCodes.Ret);
        var contract = typeof(Concept).GetProperty("EqualityContract", Members)!.GetMethod!;
        var get = Override(builder, contract);
        il = get.GetILGenerator();
        il.Emit(OpCodes.Ldtoken, builder);
        il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
        il.Emit(OpCodes.Ret);
        foreach (var method in typeof(Concept).GetMethods(Members).Where(m => m.IsAbstract)) {
            il = Override(builder, method).GetILGenerator();
            if (method.ReturnType == typeof(Concept)) {
                il.Emit(OpCodes.Newobj, constructor);
                il.Emit(OpCodes.Ret);
            } else {
                il.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes)!);
                il.Emit(OpCodes.Throw);
            }
        }
    }

    static MethodBuilder Override(TypeBuilder builder, MethodInfo method) {
        var attributes = (method.Attributes & ~MethodAttributes.Abstract & ~MethodAttributes.NewSlot)
            | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        var result = builder.DefineMethod(method.Name, attributes, method.ReturnType,
            [.. method.GetParameters().Select(p => p.ParameterType)]);
        builder.DefineMethodOverride(result, method);
        return result;
    }
}
