using ArchUnitNET.Domain;
using Viewer.Export.Model;
using VSA.Architecture.Engine;

namespace Viewer.Export;

internal static class Findings
{
    public static IEnumerable<FindingData> Of(
        ArchitectureMap architecture, IType type, string typeId, Func<string, string> relative)
    {
        return architecture.Findings(type)
            .Select(f => new FindingData(f.Code, f.File is null ? "" : relative(f.File), f.Line, f.Text, typeId));
    }
}
