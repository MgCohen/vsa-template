using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VSA.Infrastructure.EfCore;

// A strongly typed id is a struct with one Value property and one constructor that takes it,
// for example "readonly record struct NoteId(Guid Value)". It is stored as its Value.
static class StronglyTypedId {
    public static ValueConverter? Converter(Type type) {
        if (!type.IsValueType || type.GetProperty("Value") is not { } value || type.GetConstructor([value.PropertyType]) is not { } constructor) {
            return null;
        }
        var id = Expression.Parameter(type);
        var raw = Expression.Parameter(value.PropertyType);
        var toProvider = Expression.Lambda(Expression.Property(id, value), id);
        var fromProvider = Expression.Lambda(Expression.New(constructor, raw), raw);
        var converterType = typeof(ValueConverter<,>).MakeGenericType(type, value.PropertyType);
        return (ValueConverter)Activator.CreateInstance(converterType, toProvider, fromProvider, null)!;
    }
}
