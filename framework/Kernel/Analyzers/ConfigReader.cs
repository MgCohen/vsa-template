namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

static class ConfigReader {
    const string LoadContextType = "System.Runtime.Loader.AssemblyLoadContext";
    const string ConfigAssembly = "Umpire.Config";
    const string ConfigType = "Umpire.Config.UmpireConfig";
    const string ResourcePrefix = "UmpireConfigTree/";

    static readonly Lazy<Reader> Loaded = new(Load);

    public static IReadOnlyList<KeyValuePair<string, string>> Values(string toml) {
        var reader = Loaded.Value;
        try {
            var config = reader.Parse.Invoke(null, [toml]);
            return [.. (IEnumerable<KeyValuePair<string, string>>)reader.Values.Invoke(config, null)!];
        } catch (TargetInvocationException e) when (e.InnerException is { } inner) {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    public static IReadOnlyList<string> Embedded() =>
        [.. typeof(ConfigReader).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(name => name.Substring(ResourcePrefix.Length))];

    static Reader Load() {
        try {
            return LoadInPrivateContext();
        } catch (Exception e) when (e is not (FileLoadException or FileNotFoundException)) {
            throw new FileLoadException(
                $"{ConfigAssembly} cannot load in a private load context: {(e as TargetInvocationException)?.InnerException?.Message ?? e.Message}", e);
        }
    }

    static Reader LoadInPrivateContext() {
        var contextType = typeof(object).Assembly.GetType(LoadContextType)
            ?? Type.GetType(LoadContextType + ", System.Runtime.Loader")
            ?? throw new FileLoadException(
                $"The compiler runtime has no {LoadContextType}, so {ConfigAssembly} cannot load in a private load context.");
        var owner = contextType.GetMethod("GetLoadContext", [typeof(Assembly)])!.Invoke(null, [typeof(ConfigReader).Assembly]);
        var ownerIsCollectible = owner is not null && (bool)contextType.GetProperty("IsCollectible")!.GetValue(owner)!;
        var context = Activator.CreateInstance(contextType, ConfigAssembly + ".Private", ownerIsCollectible)!;
        var fromStream = contextType.GetMethod("LoadFromStream", [typeof(Stream)])!;
        var tree = new Tree(context, fromStream);
        var resolving = Subscribe(contextType, context, "Resolving", tree, nameof(Tree.Resolve));
        if (ownerIsCollectible) {
            var lifetime = new Lifetime(context, contextType.GetEvent("Resolving")!, resolving, contextType.GetMethod("Unload", Type.EmptyTypes)!);
            Subscribe(contextType, owner!, "Unloading", lifetime, nameof(Lifetime.Unload));
        }
        var config = tree.Load(ConfigAssembly)
            ?? throw new FileNotFoundException($"The analyzer DLL has no embedded {ConfigAssembly}.dll.", ConfigAssembly + ".dll");
        var type = config.GetType(ConfigType, throwOnError: true)!;
        return new Reader(type.GetMethod("Parse", [typeof(string)])!, type.GetMethod("Values", Type.EmptyTypes)!);
    }

    static Delegate Subscribe(Type contextType, object source, string eventName, object target, string method) {
        var handler = contextType.GetEvent(eventName)!;
        var subscription = Delegate.CreateDelegate(handler.EventHandlerType!, target, target.GetType().GetMethod(method)!);
        handler.AddEventHandler(source, subscription);
        return subscription;
    }

    sealed class Lifetime(object context, EventInfo resolving, Delegate resolver, MethodInfo unload) {
        public void Unload(object _) {
            resolving.RemoveEventHandler(context, resolver);
            unload.Invoke(context, null);
        }
    }

    sealed class Reader(MethodInfo parse, MethodInfo values) {
        public MethodInfo Parse { get; } = parse;

        public MethodInfo Values { get; } = values;
    }

    sealed class Tree(object context, MethodInfo fromStream) {
        readonly Dictionary<string, Assembly?> loaded = new(StringComparer.OrdinalIgnoreCase);

        public Assembly? Resolve(object _, AssemblyName name) => name.Name is { } simple ? Load(simple) : null;

        public Assembly? Load(string simpleName) {
            lock (loaded) {
                if (!loaded.TryGetValue(simpleName, out var assembly)) {
                    assembly = FromResource(simpleName);
                    loaded.Add(simpleName, assembly);
                }
                return assembly;
            }
        }

        Assembly? FromResource(string simpleName) {
            using var stream = typeof(ConfigReader).Assembly.GetManifestResourceStream(ResourcePrefix + simpleName + ".dll");
            return stream is null ? null : (Assembly)fromStream.Invoke(context, [stream])!;
        }
    }
}
