using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed class ConfigReaderLifetimeTests {
    [Test]
    public void The_private_load_context_unloads_with_its_analyzer() {
        var contexts = ReadInUnloadableHostThenUnload();
        for (var i = 0; i < 20 && contexts.Any(context => context.IsAlive); i++) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Assert.That(contexts.Select(context => context.IsAlive), Is.EqualTo(new[] { false, false }));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference[] ReadInUnloadableHostThenUnload() {
        var host = new AssemblyLoadContext("AnalyzerHost", isCollectible: true);
        var analyzer = host.LoadFromAssemblyPath(typeof(ConfigValues).Assembly.Location);
        var values = analyzer.GetType(typeof(ConfigValues).FullName!)!.GetMethod(nameof(ConfigValues.Parse))!.Invoke(null, ["a = 1"])!;
        Assert.That(values.GetType().GetMethod(nameof(ConfigValues.Value))!.Invoke(values, ["a"]), Is.EqualTo("1"));
        var privateContext = AssemblyLoadContext.All.Single(context =>
            context.Name == "Umpire.Config.Private" && context.IsCollectible);
        host.Unload();
        return [new WeakReference(host), new WeakReference(privateContext)];
    }
}
