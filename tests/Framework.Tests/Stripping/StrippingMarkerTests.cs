using System.Reflection;
using VSA.Analyzers.Markers;
using VSA.Composition;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;
using VSA.Kernel;

namespace Framework.Tests.Stripping;

public sealed class StrippingMarkerTests {
    [TestCase(typeof(PreserveAttribute), "PreserveAttribute", AttributeTargets.All)]
    [TestCase(typeof(RequireDerivedAttribute), "RequireDerivedAttribute", AttributeTargets.Class)]
    [TestCase(typeof(RequireImplementorsAttribute), "RequireImplementorsAttribute", AttributeTargets.Interface)]
    public void The_UnityLinker_finds_each_marker_by_its_class_name(Type marker, string linkerName, AttributeTargets targets) {
        Assert.That((marker.Name, marker.GetCustomAttribute<AttributeUsageAttribute>()?.ValidOn ?? AttributeTargets.All),
            Is.EqualTo((linkerName, targets)));
    }

    [TestCase(typeof(CommandHandler<,>))]
    [TestCase(typeof(RequestHandler<,>))]
    [TestCase(typeof(IntegrationEventHandler<>))]
    [TestCase(typeof(Validator<>))]
    [TestCase(typeof(Before<>))]
    [TestCase(typeof(After<>))]
    [TestCase(typeof(BeforeStep<>))]
    [TestCase(typeof(AfterStep<>))]
    [TestCase(typeof(Presenter))]
    [TestCase(typeof(VSA.Module.Module))]
    [TestCase(typeof(VsaApp<>))]
    public void A_base_class_that_the_scan_reads_keeps_its_derived_classes(Type scannedBase) {
        Assert.That(scannedBase.IsDefined(typeof(RequireDerivedAttribute), inherit: false), Is.True);
    }

    [TestCase(typeof(IAutoRegister))]
    [TestCase(typeof(ITypeArgument))]
    [TestCase(typeof(IAppHost))]
    [TestCase(typeof(IRunner))]
    public void An_interface_that_the_scan_reads_keeps_its_implementors(Type scannedInterface) {
        Assert.That(scannedInterface.IsDefined(typeof(RequireImplementorsAttribute), inherit: false), Is.True);
    }

    [TestCase("VSA.Handlers.IEnvelopeFactory")]
    [TestCase("VSA.Handlers.IBeforeCall")]
    [TestCase("VSA.Handlers.IAfterCall")]
    public void An_internal_interface_that_the_pipeline_creates_by_reflection_keeps_its_implementors(string name) {
        var internalInterface = typeof(IRunner).Assembly.GetType(name, throwOnError: true)!;

        Assert.That(internalInterface.IsDefined(typeof(RequireImplementorsAttribute), inherit: false), Is.True);
    }

    [Test]
    public void The_module_scan_names_the_marker_types_in_code_so_the_linker_keeps_their_assembly() {
        var references = typeof(MarkedTypes).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.That(references, Does.Contain(typeof(AutoRegisterAttribute).Assembly.GetName().Name));
        Assert.That(MarkedTypes.AutoRegisterName, Is.EqualTo(typeof(AutoRegisterAttribute).FullName));
    }
}
