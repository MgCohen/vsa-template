namespace Spike.Pack;

using System.Reflection;

public sealed class ReflectedConstructors : IConstructorFactory {
    public Func<object?[], object> Create(ConstructorInfo constructor) => constructor.Invoke;
}
