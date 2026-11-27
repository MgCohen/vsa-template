namespace Spike.Pack;

using System.Reflection;

public interface IConstructorFactory {
    Func<object?[], object> Create(ConstructorInfo constructor);
}
