namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;

public interface ITestHost {
    List<Action<IServiceCollection>> Actions { get; }
}
