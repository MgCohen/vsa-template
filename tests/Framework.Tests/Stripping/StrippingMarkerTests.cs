using System.Reflection;
using VSA.Analyzers.Markers;
using VSA.Composition;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Presenters;

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
    [TestCase(typeof(Presenter))]
    [TestCase(typeof(VSA.Module.Module))]
    public void A_base_class_that_the_scan_reads_keeps_its_derived_classes(Type scannedBase) {
        Assert.That(scannedBase.IsDefined(typeof(RequireDerivedAttribute), inherit: false), Is.True);
    }

    [TestCase(typeof(IAutoRegister))]
    [TestCase(typeof(ITypeArgument))]
    [TestCase(typeof(IVsaHost))]
    public void An_interface_that_the_scan_reads_keeps_its_implementors(Type scannedInterface) {
        Assert.That(scannedInterface.IsDefined(typeof(RequireImplementorsAttribute), inherit: false), Is.True);
    }
}
