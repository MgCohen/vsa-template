using Tests.Shared;

namespace CheckModel.Tests;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Umpire.CheckModel;
using Umpire.CheckModel.Analyzer;

public sealed class SdkCodeTests {
    static IEnumerable<string> DataCodeValues => typeof(DataCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (string)field.GetValue(null)!);

    [Test]
    public void The_code_list_of_the_SDK_has_each_code_of_the_check_model() {
        Assert.That(RuleCodes.All.Concat(DataCodeValues).Except(SdkCodes.All), Is.Empty);
        Assert.That(SdkCodes.All.Where(code => code.StartsWith("UMP0308", StringComparison.Ordinal)), Is.EquivalentTo(DataCodeValues));
    }

    [Test]
    public void Each_finding_of_the_analyzer_is_a_warning_by_default() {
        var descriptors = new CheckAnalyzer().SupportedDiagnostics;

        Assert.That(descriptors.Select(d => d.Id).Except(SdkCodes.All), Is.Empty);
        Assert.That(descriptors.Select(d => d.DefaultSeverity), Is.All.EqualTo(DiagnosticSeverity.Warning));
    }
}
