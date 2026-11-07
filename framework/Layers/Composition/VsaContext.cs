using Microsoft.Extensions.Configuration;
using VSA.Module;

namespace VSA.Composition;

public sealed class VsaContext(IVsaServices services, IConfiguration configuration, IReadOnlyList<VSA.Module.Module> modules) {
    public IVsaServices Services => services;
    public IConfiguration Configuration => configuration;
    public IReadOnlyList<VSA.Module.Module> Modules => modules;
}
