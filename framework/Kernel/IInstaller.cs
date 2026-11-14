using Microsoft.Extensions.DependencyInjection;
using VSA.Analyzers.Markers;

namespace VSA.Kernel;

[RequireImplementors]
public interface IInstaller {
    void Install(IServiceCollection services, AnchorTypes types);
}
