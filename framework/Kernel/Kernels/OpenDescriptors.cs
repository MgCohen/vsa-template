namespace Umpire.Kernels;

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

sealed class OpenDescriptors {
    readonly IServiceCollection services;
    readonly AppTypes types;
    readonly List<ServiceDescriptor> open;
    readonly HashSet<ServiceDescriptor> closedSome = new(ByReference.Instance);
    readonly HashSet<ServicePair> known = [];
    readonly HashSet<Type> walked = [];
    readonly Queue<Type> queue = new();

    OpenDescriptors(IServiceCollection services, AppTypes types, HashSet<ServiceDescriptor> kernelServices) {
        this.services = services;
        this.types = types;
        open = [.. services.Where(d => !kernelServices.Contains(d) && IsOpen(d))];
        foreach (var descriptor in services) {
            if (Pair(descriptor) is { } pair) {
                known.Add(pair);
                queue.Enqueue(pair.Implementation);
            }
        }
    }

    internal static HashSet<ServiceDescriptor> Snapshot(IServiceCollection services) =>
        new(services, ByReference.Instance);

    internal static void Close(
        IServiceCollection services, AppTypes types, HashSet<ServiceDescriptor> kernelServices) {
        var closing = new OpenDescriptors(services, types, kernelServices);
        closing.CloseOverAutoCloseTypes();
        closing.Walk();
        closing.RemoveClosedOpenDescriptors();
        RemoveDuplicates(services);
        StopIfAnOpenClassHasNoClosedType(services, types);
    }

    static void RemoveDuplicates(IServiceCollection services) {
        var seen = new HashSet<ServicePair>();
        for (var i = services.Count - 1; i >= 0; i--) {
            if (Pair(services[i]) is { } pair && !seen.Add(pair)) {
                services.RemoveAt(i);
            }
        }
    }

    static void StopIfAnOpenClassHasNoClosedType(IServiceCollection services, AppTypes types) {
        var closedDefinitions = services
            .Where(d => !d.IsKeyedService && d.ServiceType.IsConstructedGenericType)
            .Select(d => d.ServiceType.GetGenericTypeDefinition())
            .ToHashSet();
        var unclosed = types.OpenRegistered().FirstOrDefault(definition => !closedDefinitions.Contains(definition));
        if (unclosed is not null) {
            var parameters = string.Join(", ", unclosed.GetGenericArguments().Select(p => p.Name));
            throw new InvalidOperationException(
                $"{OpenGenerics.NameOf(unclosed)} has no type argument. "
                + $"Mark a type that meets the constraints of {parameters} with [AutoClose], "
                + "or remove [AutoRegister].");
        }
    }

    static bool IsOpen(ServiceDescriptor descriptor) =>
        !descriptor.IsKeyedService
        && descriptor.ServiceType.IsGenericTypeDefinition
        && descriptor.ImplementationType is { IsGenericTypeDefinition: true };

    static ServicePair? Pair(ServiceDescriptor descriptor) =>
        AutoRegistration.ImplementationOf(descriptor) is { ContainsGenericParameters: false } implementation
        && !descriptor.ServiceType.ContainsGenericParameters
            ? new ServicePair(descriptor.ServiceType, implementation)
            : null;

    static Type DefinitionOf(Type type) => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

    static Type ItemOf(Type type) =>
        type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type.GetGenericArguments()[0]
            : type;

    static IEnumerable<ParameterInfo> ParametersOf(Type type) {
        try {
            return [.. type.GetConstructors().SelectMany(c => c.GetParameters())];
        } catch (Exception exception)
            when (exception is TypeLoadException or FileNotFoundException or NotSupportedException) {
            return [];
        }
    }

    void CloseOverAutoCloseTypes() {
        foreach (var source in open) {
            foreach (var closed in OpenGenerics.CloseOver(source.ImplementationType!, types.AutoCloseTypes())) {
                var matching = OpenGenerics.Implemented(closed).Where(s => DefinitionOf(s) == source.ServiceType);
                foreach (var service in matching) {
                    Add(source, service, closed);
                }
            }
        }
        foreach (var source in open) {
            foreach (var descriptor in services.ToList()) {
                if (Pair(descriptor) is { } pair && DefinitionOf(pair.Service) == source.ServiceType
                    && DefinitionOf(pair.Implementation) == source.ImplementationType) {
                    closedSome.Add(source);
                }
            }
        }
    }

    void Walk() {
        while (queue.Count > 0) {
            var type = queue.Dequeue();
            if (!walked.Add(type)) {
                continue;
            }
            foreach (var parameter in ParametersOf(type)) {
                Request(ItemOf(parameter.ParameterType));
            }
        }
    }

    void Request(Type service) {
        if (!service.IsConstructedGenericType || service.ContainsGenericParameters) {
            return;
        }
        var definition = service.GetGenericTypeDefinition();
        foreach (var source in open.Where(d => d.ServiceType == definition)) {
            if (OpenGenerics.Unify(source.ImplementationType!, service) is { } implementation) {
                Add(source, service, implementation);
            }
        }
        foreach (var openClass in types.OpenRegistered()) {
            if (OpenGenerics.Unify(openClass, service) is { } closed
                && (closed == service || MarkedTypes.ServicesOf(closed).Contains(service))
                && !services.Any(d => !d.IsKeyedService && d.ServiceType == closed)) {
                AutoRegistration.AddClosed(services, closed);
                queue.Enqueue(closed);
            }
        }
    }

    void Add(ServiceDescriptor source, Type service, Type implementation) {
        closedSome.Add(source);
        if (known.Add(new ServicePair(service, implementation))) {
            services.Add(new ServiceDescriptor(service, implementation, source.Lifetime));
            queue.Enqueue(implementation);
        }
    }

    void RemoveClosedOpenDescriptors() {
        foreach (var source in open.Where(closedSome.Contains)) {
            services.Remove(source);
        }
    }

    readonly record struct ServicePair(Type Service, Type Implementation);

    sealed class ByReference : IEqualityComparer<ServiceDescriptor> {
        public static readonly ByReference Instance = new();

        public bool Equals(ServiceDescriptor? x, ServiceDescriptor? y) => ReferenceEquals(x, y);

        public int GetHashCode(ServiceDescriptor obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
