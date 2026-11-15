using System.Reflection;
using VSA.Analyzers.Markers;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Presenters;
using VSA.Kernel;

namespace Framework.Tests.Stripping;

public sealed class StrippingMarkerTests {
    [TestCase(typeof(PreserveAttribute), "PreserveAttribute", AttributeTargets.All)]
    [TestCase(typeof(RequireDerivedAttribute), "RequireDerivedAttribute", AttributeTargets.Class)]
    [TestCase(typeof(RequireImplementorsAttribute), "RequireImplementorsAttribute", AttributeTargets.Interface)]
    public void The_UnityLinker_finds_each_marker_by_its_class_name(Type marker, string linkerName, AttributeTargets targets) {
        Assert.That(marker.Name, Is.EqualTo(linkerName));
        Assert.That(marker.GetCustomAttribute<AttributeUsageAttribute>()?.ValidOn ?? AttributeTargets.All, Is.EqualTo(targets));
    }

    [TestCase(typeof(CommandHandler<,>))]
    [TestCase(typeof(RequestHandler<,>))]
    [TestCase(typeof(IntegrationEventHandler<>))]
    [TestCase(typeof(Validator<>))]
    [TestCase(typeof(BeforeCommandHook<>))]
    [TestCase(typeof(AfterCommandHook<,>))]
    [TestCase(typeof(BeforeEventHook<>))]
    [TestCase(typeof(AfterEventHook<>))]
    [TestCase(typeof(BeforeStep))]
    [TestCase(typeof(AfterStep))]
    [TestCase(typeof(BeforeStep<>))]
    [TestCase(typeof(AfterStep<>))]
    [TestCase(typeof(Presenter))]
    [TestCase(typeof(VSA.Kernel.Module))]
    [TestCase(typeof(App))]
    [TestCase(typeof(Pack))]
    [TestCase(typeof(VSA.Architecture.Architecture))]
    [TestCase(typeof(AppHost))]
    public void A_base_class_that_the_scan_reads_keeps_its_derived_classes(Type scannedBase) {
        Assert.That(scannedBase.IsDefined(typeof(RequireDerivedAttribute), inherit: false), Is.True);
    }

    [TestCase(typeof(IAutoRegister))]
    [TestCase(typeof(ITypeArgument))]
    [TestCase(typeof(IApp))]
    [TestCase(typeof(IRunner))]
    [TestCase(typeof(IBeforeCall))]
    [TestCase(typeof(IAfterCall))]
    public void An_interface_that_the_scan_reads_keeps_its_implementors(Type scannedInterface) {
        Assert.That(scannedInterface.IsDefined(typeof(RequireImplementorsAttribute), inherit: false), Is.True);
    }

    [TestCase(typeof(BeforeCommandHook<>))]
    [TestCase(typeof(AfterCommandHook<,>))]
    [TestCase(typeof(BeforeEventHook<>))]
    [TestCase(typeof(AfterEventHook<>))]
    [TestCase(typeof(BeforeStep))]
    [TestCase(typeof(AfterStep))]
    [TestCase(typeof(BeforeStep<>))]
    [TestCase(typeof(AfterStep<>))]
    public void A_stage_base_keeps_its_members(Type stageBase) {
        Assert.That(stageBase.IsDefined(typeof(PreserveAttribute), inherit: false), Is.True);
    }

    [Test]
    public void The_envelope_create_method_that_the_pipeline_closes_by_reflection_is_kept() {
        var route = typeof(IRunner).Assembly.GetType("VSA.Handlers.MessageRoute", throwOnError: true)!;
        var create = route.GetMethod("CreateEnvelope", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.That(create.IsDefined(typeof(PreserveAttribute), inherit: false), Is.True);
    }

    [Test]
    public void The_module_scan_names_the_marker_types_in_code_so_the_linker_keeps_their_assembly() {
        var references = typeof(MarkedTypes).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.That(references, Does.Contain(typeof(AutoRegisterAttribute).Assembly.GetName().Name));
        Assert.That(MarkedTypes.AutoRegisterName, Is.EqualTo(typeof(AutoRegisterAttribute).FullName));
    }
}
