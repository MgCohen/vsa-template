namespace Spike.Pack;

using System.Linq.Expressions;
using System.Reflection;

public sealed class CompiledConstructors : IConstructorFactory {
    public Func<object?[], object> Create(ConstructorInfo constructor) {
        var input = Expression.Parameter(typeof(object?[]));
        var arguments = constructor.GetParameters().Select((p, i) =>
            Expression.Convert(Expression.ArrayIndex(input, Expression.Constant(i)), p.ParameterType));
        return Expression.Lambda<Func<object?[], object>>(
            Expression.Convert(Expression.New(constructor, arguments), typeof(object)), input).Compile();
    }
}
