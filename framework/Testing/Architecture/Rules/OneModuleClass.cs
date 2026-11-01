namespace VSA.Testing.Architecture.Rules;

// VSA2003: each module root has exactly one Module class, and it is public and sealed.
// The host also fails at start-up for zero or two Module classes. This rule finds it before the app starts.
public sealed class OneModuleClass : IVsaRule {
    public string Id => "VSA2003";
    public string Title => "Each module root has one public sealed Module class";

    public IEnumerable<Violation> Check(VsaApp app) {
        foreach (var module in app.Modules) {
            var found = VsaRules.TypesOf(module.Root)
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(VSA.Module.Module)))
                .ToList();
            if (found.Count == 0) {
                yield return new(Id, $"Module root {module.Root.GetName().Name} has no Module class. Add one public sealed class that inherits Module or Module<TSettings>.");
            } else if (found.Count > 1) {
                yield return new(Id, $"Module root {module.Root.GetName().Name} has {found.Count} Module classes: {string.Join(", ", found.Select(t => t.FullName))}. Keep one.");
            } else if (found[0] is { IsPublic: false } or { IsSealed: false }) {
                yield return new(Id, $"{found[0].FullName} must be public and sealed.");
            }
        }
    }
}
