using VSA.Testing.Architecture.Rules;
using Xunit;

namespace VSA.Testing.Architecture;

// All VSA architecture rules as xUnit v3 tests. A consumer test project needs one class:
//   public sealed class ArchitectureTests : VsaArchitectureTests<Program>;
// TEntry is a type in the host assembly. The modules come from the project libraries of the test run.
public abstract class VsaArchitectureTests<TEntry> {
    static readonly Lazy<VsaApp> Shared = new(VsaApp.Of<TEntry>);

    protected VsaApp App => Shared.Value;

    // With no modules, each other rule passes with nothing to check. That is a wrong pass.
    [Fact]
    public void The_app_has_modules() {
        if (App.Modules.Count == 0) {
            throw new ArchitectureRuleException(
                $"No module root found for {typeof(TEntry).Assembly.GetName().Name}. Add a project reference to each module, and build the modules with VSA.Sdk.");
        }
    }

    [Fact]
    public void VSA2001_each_request_has_one_handler() => VsaRules.Assert(new OneHandlerPerRequest(), App);

    [Fact]
    public void VSA2002_requests_have_unique_routes() => VsaRules.Assert(new UniqueRoutes(), App);

    [Fact]
    public void VSA2003_each_module_root_has_one_Module_class() => VsaRules.Assert(new OneModuleClass(), App);

    [Fact]
    public void VSA2004_module_projects_follow_the_layer_map() => VsaRules.Assert(new LayerReferences(), App);
}
